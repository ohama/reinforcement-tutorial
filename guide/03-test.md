# 테스트

## 테스트 프레임워크

모든 Phase는 동일한 테스트 스택을 사용합니다:

- **Expecto 10.2.3** — F# 네이티브 테스트 프레임워크
- **FsCheck 2.16.5** — Property-Based Testing (무작위 입력으로 불변 조건 검증)
- **YoloDev.Expecto.TestSdk 0.15.5** — `dotnet test` 통합

## Phase별 테스트 실행

```bash
# Phase 1: 슬롯머신 (8개 테스트)
cd Bandit && dotnet test

# Phase 2: 틱택토 (8개 테스트)
cd TicTacToe && dotnet test

# Phase 3: 커넥트 포 (20개 테스트)
cd 03-connect-four && dotnet test

# Phase 4: 커넥트 포 DQN (9개 테스트)
cd 04-connect-four-dqn && dotnet test

# Phase 5: 오목 MCTS (14개 테스트)
cd 05-gomoku-mcts && dotnet test
```

## 전체 테스트 한 번에 실행

```bash
for dir in Bandit TicTacToe 03-connect-four 04-connect-four-dqn 05-gomoku-mcts; do
  echo "=== Testing $dir ==="
  (cd "$dir" && dotnet test)
done
```

## 테스트 출력 해석

### 성공 시

```
Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8, Duration: 1 s
```

### 실패 시

```
Failed!  - Failed:     1, Passed:     7, Skipped:     0, Total:     8, Duration: 1 s

  Failed PropertyTests.보상 확률 0이면 보상 항상 0 [3 ms]
  Error Message:
   FsCheck found a counterexample...
```

FsCheck 실패 시 반례(counterexample)가 출력됩니다. 이 값을 재현에 사용할 수 있습니다.

## 테스트 종류

### Property-Based Tests (FsCheck)

게임 규칙의 불변 조건을 무작위 입력으로 검증합니다:

- 보드 크기 불변
- 합법적 수의 조건
- 승리 판정 정확성
- 보상 범위 검증

### Convergence Tests

학습 알고리즘이 수렴하는지 확인합니다:

- ε-greedy가 최적 arm을 찾는지 (Phase 1)
- TD 학습 후 승률이 임계값 이상인지 (Phase 2)
- Q-Learning 학습 진행 확인 (Phase 3)

## 개별 테스트 파일 직접 실행

Expecto 테스트 프로젝트는 실행 가능한 exe이므로 직접 실행할 수도 있습니다:

```bash
cd Bandit
dotnet run --project tests/Bandit.Tests
```

이 방식은 Expecto의 컬러 출력과 상세 로그를 볼 수 있습니다.
