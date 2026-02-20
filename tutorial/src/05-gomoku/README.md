# 5장. 오목 MCTS — 트리 탐색과 자가 대국

## Phase 4의 한계

Phase 4에서 DQN으로 Connect Four를 학습시켰다. 신경망이 Q-table의 상태 공간 문제를 해결했지만 새로운 한계가 드러났다:

- **탐색 없음**: 추론 시 단 한 번의 전방향 전파로 수를 결정한다. Minimax처럼 여러 수 앞을 내다보지 않는다.
- **보상 희소성**: 게임 종료 시에만 보상이 주어진다. 수백만 번의 게임이 없으면 신호가 너무 적다.
- **오목으로 확장 불가**: 15×15 보드(225칸)에서 DQN의 단순 ε-greedy 탐색은 사실상 의미 있는 학습을 하지 못한다.

| 방식 | 탐색 | 오목 가능성 |
|------|------|------------|
| Q-table | 없음 | 불가 (상태 폭발) |
| DQN | ε-greedy (얕음) | 어려움 |
| MCTS + 신경망 | 트리 탐색 (깊음) | AlphaZero 방식으로 가능 |

**해결책: Monte Carlo Tree Search + Policy/Value Network**

신경망이 "어디가 좋아 보이는가"를 안내하고, MCTS가 실제로 게임 트리를 탐색한다.
자가 대국(Self-play)으로 생성한 데이터를 다시 신경망 학습에 활용하면 점점 강해지는 선순환이 만들어진다.

## MCTS 개념

MCTS(Monte Carlo Tree Search)는 게임 트리를 **선택 → 확장 → 평가 → 역전파** 4단계로 반복 탐색한다.

### MCTS 4단계

```
루트 (현재 상태)
  ├── 선택 (Selection)   : PUCT 점수가 높은 자식을 반복 선택해 잎 노드까지 내려간다
  ├── 확장 (Expansion)   : 잎 노드에서 합법 수를 자식으로 추가한다
  ├── 평가 (Evaluation)  : 신경망(가치 헤드)으로 현재 상태의 승패 확률을 예측한다
  └── 역전파 (Backprop)  : 예측값을 루트까지 거슬러 올라가며 각 노드를 업데이트한다
```

충분한 시뮬레이션 후, 루트의 자식 방문 횟수를 정규화하면 **이동 확률 분포 π** 가 나온다.

### PUCT 공식

AlphaZero는 UCB1 대신 PUCT(Predictor Upper Confidence bound for Trees)를 사용한다:

```
PUCT(a) = Q(a) + c_puct × P(a) × √N_parent / (1 + N_child)
```

| 항 | 의미 |
|---|------|
| Q(a) | 현재까지 이 수를 뒀을 때의 평균 가치 (활용) |
| c_puct | 탐색 강도 조절 상수 (보통 5.0) |
| P(a) | 신경망 정책 헤드가 예측한 사전 확률 (탐색 가이드) |
| √N_parent | 부모 방문 횟수가 늘수록 탐색 폭도 증가 |
| 1 + N_child | 많이 방문한 수일수록 보너스 감소 |

Q 항은 활용(exploitation), 나머지는 탐색(exploration)을 담당한다.
P(a)가 크면 처음에는 우선 탐색되고, 방문이 쌓이면 Q로 균형을 맞춘다.

## ARM64 네이티브 라이브러리 로딩

TorchSharp는 macOS ARM64에서 네이티브 라이브러리 자동 탐색에 버그가 있다.
`AppContext.BaseDirectory/runtimes/osx-arm64/native/` 에서 직접 로드해야 한다.

```fsharp
{{#include ../../../05-gomoku-mcts/src/Gomoku/NativeLoader.fs}}
```

`do load ()` 는 모듈 레벨에서 즉시 실행되므로 어떤 TorchSharp 호출보다 먼저 라이브러리가 로드된다.

## MCTS 트리 노드

MCTS 트리는 가변(mutable) 클래스로 구현한다. 레코드(record)를 쓰면 부모 포인터와 자식 딕셔너리를 효율적으로 관리할 수 없다.

```fsharp
{{#include ../../../05-gomoku-mcts/src/Gomoku/MctsNode.fs}}
```

**핵심 설계 결정:**

- `Prior`는 mutable — 루트에서 Dirichlet 노이즈 주입 시 변경 필요
- `UpdateRecursive(-leafValue)` 호출 규약: 잎에서 값을 부정한 뒤 전달, 재귀 호출마다 다시 부정 → 교대 부호(zero-sum 관점 전환)
- 단말 노드의 `leafValue = -1.0`: 마지막으로 수를 둔 플레이어가 이겼으므로 현재 플레이어 입장에서 나쁨

## Policy/Value 신경망

AlphaZero 방식의 이중 헤드(dual-head) 신경망:

- **정책 헤드(Policy Head)**: 225개 합법 수의 사전 확률 분포 → MCTS 탐색 가이드
- **가치 헤드(Value Head)**: 현재 상태의 승패 예측 [-1, 1] → 말단 평가 대체

```fsharp
{{#include ../../../05-gomoku-mcts/src/Gomoku/PolicyValueNet.fs}}
```

**아키텍처 요약:**

```
입력: [batch, 4, 15, 15]
  채널 0: 현재 플레이어 돌
  채널 1: 상대 플레이어 돌
  채널 2: 마지막 수 위치
  채널 3: 흑 차례 여부 (1.0 전체)

공유 백본:
  Conv2d(4→32, 3×3, pad=1) → BN → ReLU
  Conv2d(32→64, 3×3, pad=1) → BN → ReLU
  Conv2d(64→128, 3×3, pad=1) → BN → ReLU

정책 헤드:
  Conv2d(128→4, 1×1) → BN → ReLU → Flatten
  Linear(4×225→225) → log_softmax → [batch, 225]

가치 헤드:
  Conv2d(128→2, 1×1) → BN → ReLU → Flatten
  Linear(2×225→256) → ReLU → Linear(256→1) → Tanh → [batch, 1]
```

**주의:** `open type TorchSharp.torch` 는 F# 내장 `float`, `int`, `int64`, `sqrt` 등을 가린다.
이 파일에서는 숫자 변환에 `Operators.int`, `Operators.int64` 를 사용한다.

## MCTS 탐색 (신경망 연동)

순수 MCTS와 신경망 연동 MCTS 두 가지 구현이 있다.

```fsharp
{{#include ../../../05-gomoku-mcts/src/Gomoku/Mcts.fs}}
```

**PUCT 선택 (`puctScore`):** Q값과 탐색 보너스를 합산해 최선의 자식을 고른다.

**Dirichlet 노이즈:** 자가 대국 학습 시에만 루트 자식에 노이즈를 추가해 탐색 다양성을 확보한다.
파이어α = 0.3이 오목 표준값. 순수 F# Marsaglia-Tsang Gamma 샘플러로 구현 (torch.distributions 대신).

**`open type TorchSharp.torch`를 모듈 레벨에서 쓰지 않는 이유:** F# 내장 `float`, `sqrt`, `log`, `cos` 등이 가려져서 수식 코드가 컴파일되지 않는다. 모든 torch 타입은 `torch.X` 로 완전 한정(fully-qualified) 접근한다.

## 자가 대국 (Self-Play)

```fsharp
{{#include ../../../05-gomoku-mcts/src/Gomoku/SelfPlay.fs}}
```

**온도(Temperature) 기반 수 선택:**

```
이동 횟수 < 15 : 확률 비례 무작위 선택 (탐색)
이동 횟수 ≥ 15 : 방문 횟수 최대 선택 (활용)
```

초반에는 다양한 게임을 탐색하고, 중반부터는 최선의 수를 고른다.

**훈련 샘플 저장:** Tensor를 버퍼에 직접 저장하면 메모리 누수가 발생한다.
`float32[]` 배열로 변환해서 저장하고, 학습 시에 다시 Tensor로 변환한다.

## 자가 대국 학습 파이프라인

AlphaZero 방식의 자가 대국 → 학습 루프:

```
for 반복 in 1..NTrainingIter:
  1. 자가 대국 (Self-Play)
     model.eval() → playSelfPlayGame() × NSelfPlayGames
     → 훈련 샘플 (상태, 정책 타겟, 가치 타겟) 생성
     → 리플레이 버퍼에 추가 (최대 MaxBufferSize 유지)

  2. 학습 (Training)
     model.train() → trainBatch() × NEpochsPerIter
     손실 = -정책 교차 엔트로피 + MSE 가치 손실
     → Adam 옵티마이저로 역전파
```

### 학습 실행

```bash
# ARM64 macOS 필수 준비:
brew install libomp

cd 05-gomoku-mcts
dotnet run --project src/Gomoku.Console
# 메뉴 → 1 (자가 대국 학습, 수 시간 소요)
# 메뉴 → 2 (벤치마크: AI vs Random, 50 게임)
# 메뉴 → 3 (사람 vs AI)
```

Serilog 구조화 로그 (`logs/gomoku-training.log`) 에서 학습 과정을 확인한다:

```
[09:15:03 INF] SelfPlay training started: Iterations=200 Simulations=100 LR=0.002
[09:16:44 INF] Training Iter=1 Games=1 PolicyLoss=5.4183 ValueLoss=0.9921
[09:25:11 INF] Training Iter=10 Games=10 PolicyLoss=3.2104 ValueLoss=0.5812
[09:42:38 INF] Training Iter=20 Games=20 PolicyLoss=2.1083 ValueLoss=0.3447
[10:18:22 INF] Model saved to gomoku_model.pt
[10:18:24 INF] Benchmark: WinRate=42.0 % Wins=21/50 Simulations=100
```

손실이 감소하면 신경망이 자가 대국 결과를 흡수하고 있는 것이다.

## Phase 5 총정리

| 구성 요소 | 역할 | 파일 |
|-----------|------|------|
| Domain.fs | 오목 상태 타입 (Board, Player, GameState) | `Gomoku.Domain` |
| Rules.fs | 합법 수 생성, 승리 판정, 상태 전이 | `Gomoku.Rules` |
| MctsNode.fs | MCTS 트리 노드 (가변 클래스) | `Gomoku.MctsNode` |
| Mcts.fs | 순수 MCTS + PUCT 신경망 탐색 | `Gomoku.Mcts` |
| PolicyValueNet.fs | 이중 헤드 신경망 (정책 + 가치) | `Gomoku.PolicyValueNet` |
| SelfPlay.fs | 자가 대국 게임 루프, 훈련 샘플 생성 | `Gomoku.SelfPlay` |
| Training.fs | 학습 파이프라인, 벤치마크 | `Gomoku.Training` |
| Program.fs | 메뉴 진입점, Serilog 구조화 로그 | `Gomoku.Console` |

**검증 전략 (FsCheck):**

```
GMOK-01 ~ 08 : Rules.fs 속성 기반 테스트 (8가지 승리 방향, 드로우, 대칭)
GMOK-09 ~ 10 : MCTS 속성 테스트 (방문 횟수 합, 순수 MCTS vs 랜덤 100% 승률)
GMOK-11      : 신경망 출력 형태 테스트 (정책 [1,225] log-probs 합, 가치 [-1,1])
```

**핵심 교훈:**

1. **MCTS는 탐색 예산을 할당하는 전략** — 신경망이 없어도 동작하지만, 신경망 가이드로 탐색 효율이 크게 향상된다.
2. **이중 헤드 신경망** — 정책과 가치를 공유 백본에서 동시에 학습하면 표현을 공유해 수렴이 빠르다.
3. **자가 대국 데이터는 자기 강화적** — 신경망이 강해질수록 더 좋은 훈련 데이터가 생성되어 선순환이 만들어진다.
4. **순수 F#의 힘** — 도메인(Rules.fs)과 알고리즘(Mcts.fs)은 순수 함수로 작성되어 테스트 가능하다. 부수 효과는 Program.fs 한 곳에만 있다.

---

이 튜토리얼에서는 슬롯머신의 단순 탐색-활용 딜레마에서 시작해, 테이블 기반 강화학습(TD, Q-Learning), 신경망 함수 근사(DQN), 그리고 AlphaZero 방식의 MCTS + 자가 대국까지 단계적으로 구현했다. 각 Phase는 이전 Phase의 한계를 극복하는 방향으로 설계되었고, 모든 핵심 알고리즘은 F#의 순수 함수와 property-based test로 검증되었다.
