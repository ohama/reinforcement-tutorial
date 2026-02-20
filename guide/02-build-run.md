# 빌드 및 실행

각 Phase는 독립된 .NET solution으로, 프로젝트 루트에서 해당 디렉토리로 이동하여 실행합니다.

## Phase 1: 슬롯머신 (Bandit)

ε-greedy와 UCB1 전략을 비교하는 실험을 실행합니다.

```bash
cd Bandit
dotnet run --project src/Bandit.Console
```

**출력 예시:**

```
[12:00:00 INF] === ε-greedy 비교 (1000 steps, 10-arm bandit) ===
[12:00:00 INF]   ε=0.01  최적 arm=9  추정 가치=0.900  총 보상≈870.0
[12:00:00 INF]   ε=0.10  최적 arm=9  추정 가치=0.895  총 보상≈820.0
[12:00:00 INF]   ε=0.30  최적 arm=9  추정 가치=0.888  총 보상≈710.0
[12:00:00 INF] --------------------------------------------------
[12:00:00 INF] === ε-greedy (ε=0.10) vs UCB1 ===
[12:00:00 INF]   ε-greedy 총 보상≈820.0
[12:00:00 INF]   UCB1     총 보상≈860.0
[12:00:00 INF]   승자: UCB1 (+40.0)
```

대화형 모드 없이 자동으로 실험이 완료됩니다.

## Phase 2: 틱택토 (TicTacToe)

TD Learning으로 10만 에피소드 자가 대국 학습 후, 사람 vs AI 대전이 시작됩니다.

```bash
cd TicTacToe
dotnet run --project src/TicTacToe.Console
```

**출력 예시:**

```
[12:00:00 INF] === TicTacToe TD Learning 시작 ===
[12:00:00 INF] 학습 설정: 에피소드=100,000 alpha=0.1 epsilon=0.1 로그간격=1,000
[12:00:01 INF] Episode=1000 WinRate=55.0%
...
[12:00:05 INF] 학습 완료. 최종 승률: 85.0%

=============================
 사람(X) vs AI(O) 대전 시작!
=============================
위치 안내: 1|2|3 / 4|5|6 / 7|8|9 (1=왼쪽 위, 9=오른쪽 아래)

 . | . | .
 ---------
 . | . | .
 ---------
 . | . | .
위치 입력 (1-9):
```

1~9 숫자를 입력하여 수를 놓습니다.

## Phase 3: 커넥트 포 (Connect Four)

Q-Learning 5만 에피소드 학습 후 메뉴가 표시됩니다.

```bash
cd 03-connect-four
dotnet run --project src/ConnectFour.Console
```

**출력 예시:**

```
Q-Learning 에이전트 학습 중 (50,000 에피소드)...
[12:00:00 INF] Episode=10000 QTableSize=42518 RedWinRate=35.0%
...

=== Q-Table 크기 분석 ===
학습 후 방문한 상태: 185432
전체 가능한 상태:    4531985219092
커버율:              0.000004%
(이것이 Phase 4 DQN이 필요한 이유입니다)

=== Connect Four Phase 3 ===
1. AI vs AI (Minimax vs Q-Learning, 20게임)
2. 사람 vs Minimax AI
3. 사람 vs Q-Learning AI
0. 종료
선택:
```

메뉴에서 번호를 선택합니다. 대전 시 1~7 열 번호를 입력합니다.

## Phase 4: 커넥트 포 DQN

TorchSharp 기반 DQN 신경망을 학습합니다. 첫 빌드 시 NuGet 패키지 다운로드에 시간이 걸립니다.

```bash
cd 04-connect-four-dqn
dotnet run --project src/ConnectFourDQN.Console
```

**출력 예시:**

```
=== F# DQN Connect Four ===

1. 학습 (50K 에피소드, 커리큘럼)
2. 벤치마크 (DQN vs Minimax depth 4, 100 게임)
3. 사람 vs DQN
0. 종료
선택:
```

- **1번 (학습)**: 커리큘럼 학습 실행 (random → Minimax depth 2 → depth 4). 수 분 소요.
- **2번 (벤치마크)**: 학습된 모델이 필요합니다. 먼저 1번을 실행하세요.
- **3번 (대전)**: 사람(Y) vs DQN(R). 0~6 열 번호를 입력합니다.

## Phase 5: 오목 MCTS (Gomoku)

MCTS + 신경망 자가 대국 학습입니다.

```bash
cd 05-gomoku-mcts
dotnet run --project src/Gomoku.Console
```

**출력 예시:**

```
=== Gomoku MCTS ===
  1. Train (self-play)
  2. Benchmark vs random
  3. Human vs AI
  4. Quit
Choice:
```

- **1번 (학습)**: 자가 대국 학습 실행. 상당한 시간이 소요됩니다.
- **2번 (벤치마크)**: AI vs 랜덤 에이전트. 시뮬레이션 수를 입력합니다 (기본값 100).
- **3번 (대전)**: `row,col` 형식으로 좌표를 입력합니다 (예: `7,7`).

## 전체 빌드

모든 Phase를 한 번에 빌드만 하려면:

```bash
for dir in Bandit TicTacToe 03-connect-four 04-connect-four-dqn 05-gomoku-mcts; do
  echo "=== Building $dir ==="
  (cd "$dir" && dotnet build)
done
```
