# 4장. Connect Four DQN — 신경망으로 Q-table 한계 극복

## Phase 3의 한계

Phase 3에서 Q-Learning으로 Connect Four를 학습시켰다. 결과는 실망스러웠다:
**4조 5,319억 개(4.5T)** 의 가능한 상태 중 50,000 에피소드로 탐색한 것은 단 **0.000004%** 에 불과했다.

Q-table 방식은 "방문한 상태"만 기억한다. 비슷하게 생긴 두 보드가 있어도
한쪽을 방문하지 않았다면 Q-값은 0(기본값)이다. 보드 게임처럼 상태 공간이 광대하면
표 방식은 근본적인 한계에 부딪힌다.

| 방식 | 상태 표현 | Connect Four 성능 |
|------|-----------|-------------------|
| Q-table | 방문한 상태만 기억 | 0.000004% 탐색 |
| DQN | 신경망 파라미터로 압축 | 유사한 상태 일반화 |

**해결책: 함수 근사(Function Approximation)**

Q-table 대신 신경망 Q(s, a; θ)를 사용한다.
비슷한 보드 패턴에서 비슷한 Q-값을 출력하도록 학습하면
방문하지 않은 상태도 일반화할 수 있다.

## DQN 핵심 개념

### 1. 경험 재플레이 (Experience Replay)

연속적인 게임 데이터는 시간적으로 상관되어 있다.
이 상관성이 신경망 학습을 불안정하게 만든다.
해결책: 경험을 버퍼에 저장하고 **무작위 배치**를 샘플링한다.

### 2. 타겟 네트워크 (Target Network)

Bellman 방정식의 목표값 `r + γ · max Q(s', a')` 을 계산할 때
학습 중인 네트워크를 그대로 쓰면 "움직이는 과녁"을 쫓는 꼴이 된다.
해결책: **별도의 타겟 네트워크**를 두고 주기적으로 동기화한다.

### 3. 커리큘럼 학습 (Curriculum Learning)

| 에피소드 구간 | 상대방 |
|--------------|--------|
| 0 ~ 20,000   | 랜덤 |
| 20,000 ~ 35,000 | Minimax depth 2 |
| 35,000 ~ 50,000 | Minimax depth 4 |

약한 상대에서 강한 상대로 점진적으로 난이도를 높이면 수렴이 훨씬 빠르다.

## ARM64 네이티브 라이브러리 로딩

TorchSharp는 macOS ARM64에서 네이티브 라이브러리 자동 탐색에 버그가 있다.
`AppContext.BaseDirectory/runtimes/osx-arm64/native/` 에서 직접 로드해야 한다.

```fsharp
{{#include ../../../04-connect-four-dqn/src/ConnectFourDQN/NativeLoader.fs}}
```

## 보드 텐서 변환 (3-채널 인코딩)

보드를 **[3, 6, 7]** float32 텐서로 인코딩한다:
- 채널 0: 내 돌 위치 (1.0)
- 채널 1: 상대 돌 위치 (1.0)
- 채널 2: 빈 칸 위치 (1.0)

**불변 조건 (DQN-08):** 임의의 합법 보드에서 텐서의 모든 값 합 = 42.0
(모든 칸에서 정확히 하나의 채널이 1.0)

FsCheck property test로 검증.

에이전트 전체 코드 (보드→텐서 변환, epsilon-greedy, 학습 스텝, 타겟 동기화):

```fsharp
{{#include ../../../04-connect-four-dqn/src/ConnectFourDQN/DQNAgent.fs}}
```

## DQN 모델 구조

Connect Four 6×7 보드에 맞는 Conv2D 아키텍처:

```
입력: [batch, 3, 6, 7]
  → Conv2d(3→64, 3×3, pad=1) → ReLU → [batch, 64, 6, 7]
  → Conv2d(64→128, 3×3, pad=1) → ReLU → [batch, 128, 6, 7]
  → Flatten → [batch, 5376]  (128×6×7=5376)
  → Linear(5376→256) → ReLU
  → Linear(256→7)
출력: [batch, 7] — 7개 열의 Q-값
```

```fsharp
{{#include ../../../04-connect-four-dqn/src/ConnectFourDQN/DQNModel.fs}}
```

## Experience Replay 버퍼

텐서를 버퍼에 직접 저장하면 메모리 누수가 발생한다.
`float32[]` 배열로 변환해서 저장해야 한다.

```fsharp
{{#include ../../../04-connect-four-dqn/src/ConnectFourDQN/ReplayBuffer.fs}}
```

## 커리큘럼 학습 루프

```fsharp
{{#include ../../../04-connect-four-dqn/src/ConnectFourDQN.Console/Training.fs}}
```

## 학습 실행

```bash
# ARM64 macOS 필수 준비:
brew install libomp

cd 04-connect-four-dqn
dotnet run --project src/ConnectFourDQN.Console
# 메뉴 → 1 (학습 시작, ~수 분 소요)
# 메뉴 → 2 (벤치마크: DQN vs Minimax depth 4, 100 게임)
```

Serilog 로그 (`logs/dqn-training.log`) 에서 학습 과정을 확인한다:

```
Episode=01000 AvgLoss=0.452891 WinRate=12.3%
Episode=10000 AvgLoss=0.198344 WinRate=34.7%
Episode=30000 AvgLoss=0.087621 WinRate=52.1%
Episode=50000 AvgLoss=0.043218 WinRate=63.4%
```

손실이 감소하고 승률이 증가하면 학습이 정상적으로 진행되는 것이다.

## DQN의 한계와 Phase 5 예고

DQN은 Q-table의 상태 공간 문제를 해결했지만 새로운 한계가 있다:

- **탐색 없음**: 학습 시에는 무작위 탐색(ε-greedy)에 의존하지만
  추론 시에는 단일 전방향 전파로 수를 결정한다.
  Minimax처럼 여러 수 앞을 내다보지 않는다.
- **샘플 효율**: 수백만 번의 게임이 필요하다.
- **불안정성**: 학습이 발산할 수 있다 (타겟 네트워크, 클리핑으로 완화).

**Phase 5: MCTS + Policy/Value Network**

Monte Carlo Tree Search는 신경망 가이드 아래 실제로 게임 트리를 탐색한다.
DQN이 "직감"이라면 MCTS는 "계산된 직감" 이다.
AlphaZero는 이 조합으로 바둑, 체스, 장기를 정복했다.
