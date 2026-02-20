# F#으로 배우는 강화학습

F#으로 강화학습(Reinforcement Learning) 핵심 알고리즘을 단계별로 구현하며 배우는 실전 튜토리얼입니다.

슬롯머신(상태 없음)에서 시작해 오목 AlphaZero(MCTS + 신경망)까지, 각 단계에서 이전 방식의 한계를 극복하는 새로운 알고리즘을 구현합니다.

## 구성

| Phase | 게임 | 핵심 알고리즘 | 테스트 |
|-------|------|-------------|--------|
| 1 | 슬롯머신 (Bandit) | ε-greedy, UCB1 | 8 |
| 2 | 틱택토 | TD(0) Learning, 자가 대국 | 8 |
| 3 | 커넥트 포 | Q-Learning, Minimax + Alpha-Beta | 20 |
| 4 | 커넥트 포 DQN | Conv2D DQN, Experience Replay, Target Network | 9 |
| 5 | 오목 (Gomoku) | MCTS, PUCT, Policy/Value Network, Self-Play | 14 |

각 Phase는 **독립된 F# solution**으로, 이전 Phase에 의존하지 않습니다.

## 빠른 시작

### 필수 조건

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [mdBook](https://rust-lang.github.io/mdBook/) (튜토리얼 빌드용)

### 실행

```bash
# Phase 1: 슬롯머신
cd Bandit && dotnet run --project src/Bandit.Console

# Phase 2: 틱택토 (학습 + 대전)
cd TicTacToe && dotnet run --project src/TicTacToe.Console

# Phase 3: 커넥트 포 (Minimax vs Q-Learning)
cd 03-connect-four && dotnet run --project src/ConnectFour.Console

# Phase 4: 커넥트 포 DQN (신경망 학습)
cd 04-connect-four-dqn && dotnet run --project src/ConnectFourDQN.Console

# Phase 5: 오목 MCTS (자가 대국 학습 + 사람 vs AI)
cd 05-gomoku-mcts && dotnet run --project src/Gomoku.Console
```

### 테스트

```bash
# 각 Phase별 테스트 실행
cd Bandit && dotnet test
cd TicTacToe && dotnet test
cd 03-connect-four && dotnet test
cd 04-connect-four-dqn && dotnet test
cd 05-gomoku-mcts && dotnet test
```

### 튜토리얼 빌드

```bash
mdbook build tutorial/
open tutorial/book/index.html
```

## 프로젝트 구조

```
reinforcement-tutorial/
├── Bandit/                    # Phase 1: Multi-Armed Bandit
│   ├── src/Bandit/            #   Domain.fs, Environment.fs, Agent.fs
│   ├── src/Bandit.Console/    #   Program.fs (Serilog)
│   └── tests/Bandit.Tests/    #   FsCheck + Expecto
├── TicTacToe/                 # Phase 2: TD Learning
│   ├── src/TicTacToe/         #   Domain.fs, Rules.fs, Agent.fs
│   ├── src/TicTacToe.Console/ #   Program.fs (학습 + 사람 vs AI)
│   └── tests/TicTacToe.Tests/
├── 03-connect-four/           # Phase 3: Q-Learning + Minimax
│   ├── src/ConnectFour/       #   Domain.fs, Rules.fs, Minimax.fs, QAgent.fs
│   ├── src/ConnectFour.Console/
│   └── tests/ConnectFour.Tests/
├── 04-connect-four-dqn/       # Phase 4: DQN (TorchSharp)
│   ├── src/ConnectFourDQN/    #   DQNModel.fs, DQNAgent.fs, ReplayBuffer.fs
│   ├── src/ConnectFourDQN.Console/
│   └── tests/ConnectFourDQN.Tests/
├── 05-gomoku-mcts/            # Phase 5: MCTS + AlphaZero
│   ├── src/Gomoku/            #   MctsNode.fs, Mcts.fs, PolicyValueNet.fs
│   ├── src/Gomoku.Console/    #   Program.fs (학습 + 벤치마크 + 사람 vs AI)
│   └── tests/Gomoku.Tests/
└── tutorial/                  # 한국어 mdBook 튜토리얼
    └── src/
        ├── 01-bandit/
        ├── 02-tictactoe/
        ├── 03-connect-four/
        ├── 04-dqn/
        └── 05-gomoku/
```

## 학습 경로

```
Bandit (상태 없음)
  → "행동이 상태를 바꾸면?"
TicTacToe (작은 상태 공간)
  → "상태가 너무 많으면?"
Connect Four + Q-Learning (대규모 상태 공간)
  → "Q-table이 메모리에 안 들어가면?"
Connect Four DQN (신경망 일반화)
  → "한 수 앞만 보면?"
Gomoku MCTS (트리 탐색 + 신경망)
```

## 기술 스택

- **F# 9** / .NET 10
- **TorchSharp-cpu 0.106.0** (Phase 4, 5)
- **Expecto 10.2.3** + **FsCheck 2.16.5** (property-based testing)
- **Serilog** (구조화 로깅)
- **mdBook** (튜토리얼 사이트)

## 설계 원칙

- **Pure Core / Impure Shell**: 게임 엔진(Domain.fs, Rules.fs)에 I/O 없음. Program.fs만 입출력 담당.
- **Copy-and-Evolve**: 각 Phase는 독립 solution. 공유 라이브러리 없음.
- **Property-Based Testing**: FsCheck로 게임 규칙 불변 조건 검증.
- **Option/Result 패턴**: 예외 throw 대신 F# 타입 시스템으로 에러 처리.
