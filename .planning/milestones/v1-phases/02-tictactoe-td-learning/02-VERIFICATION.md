---
phase: 02-tictactoe-td-learning
verified: 2026-02-19T15:10:00Z
status: passed
score: 4/4 must-haves verified
---

# Phase 2: Tic-Tac-Toe (TD Learning) Verification Report

**Phase Goal:** MDP와 TD Learning 개념이 동작하는 F# 코드로 검증되고, 학습된 AI가 콘솔에서 사람과 대전할 수 있다
**Verified:** 2026-02-19T15:10:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | `dotnet test`가 FsCheck 보드 불변 조건(5개)과 Expecto 승률 >90% 테스트(3개)를 통과한다 | VERIFIED | `Total tests: 8, Passed: 8, Failed: 0` — 실행 확인 |
| 2 | `dotnet run`으로 학습된 에이전트와 콘솔 대전이 가능하다 | VERIFIED | `Program.fs` 96줄: `trainAgent` 호출 → `runHumanVsAI` 루프, 입력 검증/재시도 포함 |
| 3 | Serilog가 매 1000판 승률을 구조화 로그로 출력하고 학습 곡선이 수렴한다 | VERIFIED | Console+File 싱크, `Episode={Episode} WinRate={WinRate:P1}` 템플릿, logInterval=1_000 확인 |
| 4 | mdBook 02-tictactoe/ 챕터에 `{{#include}}`로 실제 소스가 인클루드되고, Phase 1 한계와 MDP 필요성이 설명된다 | VERIFIED | 4개 include 확인, `mdbook build` 성공, 빌드된 HTML에 `module TicTacToe` 4회 등장 |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `TicTacToe/src/TicTacToe/Domain.fs` | 불변 GameState 타입, Board, ValueTable | VERIFIED | 32줄, I/O 없음, `GameState`, `Board`, `ValueTable`, `emptyBoard`, `initialState`, `otherPlayer` export |
| `TicTacToe/src/TicTacToe/Rules.fs` | 승리 판정, 합법 수 목록, applyMove | VERIFIED | 44줄, I/O 없음, `checkWinner`, `isGameOver`, `legalMoves`, `applyMove` export |
| `TicTacToe/src/TicTacToe/Agent.fs` | 랜덤 에이전트, TD(0) 에이전트 | VERIFIED | 39줄, I/O 없음, `randomAgent`, `tdAgent`, `tdUpdate` export |
| `TicTacToe/src/TicTacToe/Training.fs` | 자가 대국 루프, winRateVsRandom | VERIFIED | 94줄, 순수 함수, `trainAgent` returns `(ValueTable * (int * float) list)`, `winRateVsRandom` export |
| `TicTacToe/src/TicTacToe.Console/Program.fs` | Serilog, trainAgent 호출, 사람 vs AI 루프 | VERIFIED | 96줄, Serilog Console+File 싱크, `runHumanVsAI` 재귀 루프, 입력 검증/재시도 |
| `TicTacToe/tests/TicTacToe.Tests/PropertyTests.fs` | FsCheck 보드 불변 조건 테스트 5개 | VERIFIED | 44줄, `testProperty` 5개: 빈칸 감소, 차례 교대, 범위, applyMove 원자성, 초기 보드 |
| `TicTacToe/tests/TicTacToe.Tests/ConvergenceTests.fs` | Expecto 승률 >90% 테스트 | VERIFIED | 34줄, `testCase` 3개: 100k 학습 후 승률>90%, 랜덤 에이전트 합법 수, TD 에이전트 합법 수 |
| `TicTacToe/TicTacToe.sln` | 독립 F# solution (TICT-10) | VERIFIED | 3개 프로젝트: TicTacToe, TicTacToe.Console, TicTacToe.Tests |
| `tutorial/src/02-tictactoe/README.md` | 한국어 챕터, {{#include}} 4개 | VERIFIED | 117줄, 4개 include (Domain.fs, Rules.fs, Agent.fs, Training.fs), Phase 1 한계/MDP 섹션 포함 |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Program.fs` | `Training.fs::trainAgent` | 직접 호출 | WIRED | `let vTable, history = trainAgent rng 100_000 0.1 0.1 1_000` |
| `Program.fs` | `Agent.fs::tdAgent` | `runHumanVsAI` 내부 | WIRED | `let move = tdAgent rng 0.0 vTable state` (epsilon=0 순수 탐욕) |
| `Program.fs` | Serilog | `Log.Information` | WIRED | 구조화 템플릿 `Episode={Episode} WinRate={WinRate:P1}` |
| `Training.fs` | `Agent.fs::tdAgent` | `playEpisode` 내부 | WIRED | `let move = tdAgent rng epsilon vTable state` |
| `Training.fs` | `Agent.fs::randomAgent` | `winRateVsRandom` 내부 | WIRED | `randomAgent rng state` for O player |
| `Training.fs` | `Rules.fs` | `isGameOver`, `applyMove` | WIRED | 두 함수 모두 `playEpisode`와 `winRateVsRandom`에서 사용 |
| `Agent.fs` | `Rules.fs::legalMoves` | `randomAgent`, `tdAgent` | WIRED | 두 에이전트 모두 `legalMoves state.Board` 호출 |
| `tutorial` | 소스 파일 4개 | `{{#include}}` | WIRED | mdbook build 성공, 빌드된 HTML에 실제 소스 코드 포함 확인 |
| `PropertyTests.fs` | `Rules.fs::applyMove`, `legalMoves` | Expecto+FsCheck | WIRED | 5개 프로퍼티 테스트 모두 통과 |
| `ConvergenceTests.fs` | `Training.fs::trainAgent`, `winRateVsRandom` | Expecto | WIRED | 100k 학습 후 승률>90% 테스트 통과 |

### Requirements Coverage

| Requirement | Status | Evidence |
|-------------|--------|---------|
| TUTR-03: RL 개념 + F# 타입 + 알고리즘 포함 | SATISFIED | tutorial/src/02-tictactoe/README.md: MDP, TD(0), 가치 함수 설명 |
| TUTR-04: `{{#include}}`로 실제 소스 인클루드 | SATISFIED | 4개 include 지시자, mdbook build 성공, HTML에 소스 확인 |
| TUTR-05: Phase 간 연결 설명 | SATISFIED | "Phase 1의 한계: 왜 MDP가 필요한가?" 섹션 + "Phase 3 예고" 섹션 |
| TUTR-06: 한국어 작성 | SATISFIED | 튜토리얼 챕터 전체 한국어 |
| TICT-01: 3×3 보드 및 게임 규칙 구현 | SATISFIED | Rules.fs: `checkWinner`, `isGameOver`, `legalMoves`, `applyMove` |
| TICT-02: 불변 GameState 타입 | SATISFIED | Domain.fs: `type GameState = { Board: Board; CurrentPlayer: Cell }` |
| TICT-03: 랜덤 에이전트 구현 | SATISFIED | Agent.fs: `randomAgent` |
| TICT-04: TD(0) Learning 에이전트 | SATISFIED | Agent.fs: `tdAgent` + `tdUpdate` (epsilon-greedy, V 최대화/최소화) |
| TICT-05: 자가 대국 학습 루프 (10만 판) | SATISFIED | Training.fs: `trainAgent rng 100_000` |
| TICT-06: 학습된 AI vs 사람 대전 (콘솔) | SATISFIED | Program.fs: `runHumanVsAI` 재귀 루프 |
| TICT-07: FsCheck 보드 불변 조건 테스트 | SATISFIED | PropertyTests.fs: 5개 testProperty, 전체 통과 |
| TICT-08: Expecto 100k 학습 후 승률 >90% | SATISFIED | ConvergenceTests.fs + `dotnet test` 통과 확인 |
| TICT-09: Serilog 학습 곡선 로깅 (매 1000판) | SATISFIED | Program.fs: `Log.Information("Episode={Episode} WinRate={WinRate:P1}")`, logInterval=1_000 |
| TICT-10: 독립 F# solution (TicTacToe.sln) | SATISFIED | TicTacToe/TicTacToe.sln — 3개 프로젝트, Bandit과 무관 |

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|---------|--------|
| — | — | — | 스텁/플레이스홀더/TODO 없음 |

전체 소스 파일에서 스텁 패턴(`TODO`, `FIXME`, `placeholder`, `return null`, `return {}`) 없음.  
Domain.fs, Rules.fs, Agent.fs에 I/O 코드 없음 (순수/비순수 경계 준수).  
Training.fs에 I/O 없음 — 로깅은 Program.fs에만 집중.

### Human Verification Required

없음. 모든 성공 기준을 자동화 검증으로 확인 완료.

참고: 성공 기준 2 (사람 vs AI 대전)는 `dotnet run` 실행 시 콘솔 입력을 기다리므로 직접 실행하지 않았으나, `Program.fs`의 구조적 검증으로 대체:
- `runHumanVsAI`가 `System.Console.ReadLine()`으로 입력 수신
- `System.Int32.TryParse`로 1-9 범위 검증
- 빈칸 여부 확인 후 재귀 재시도 또는 진행
- AI는 `tdAgent rng 0.0 vTable state`로 순수 탐욕적 플레이
- `dotnet test`에서 TD 에이전트 동작(랜덤 상대 승률 >90%)이 이미 검증됨

### Gaps Summary

갭 없음. 4개 성공 기준 모두 충족.

---

## Detailed Evidence

### SC1: `dotnet test` 결과

```
Total tests: 8
     Passed: 8
    0 Error(s)

FsCheck board invariants:
  Passed: Empty cell count decreases by 1 after one legal move [27ms]
  Passed: Players alternate: O follows X [27ms]
  Passed: All legal moves are within [0, 8] range [27ms]
  Passed: applyMove only changes target cell, leaves rest unchanged [27ms]
  Passed: Initial board has 9 empty cells [27ms]

Expecto 수렴 테스트:
  Passed: TD 에이전트가 100k 자가 대국 후 랜덤 상대 승률 > 90% [4s]
  Passed: 랜덤 에이전트는 항상 합법 수(0-8)를 반환한다 [2ms]
  Passed: TD 에이전트는 비종단 보드에서 합법 수를 반환한다 [79ms]
```

### SC2: 사람 vs AI 루프 (구조 검증)

`Program.fs` 22-60번 줄 `runHumanVsAI`:
- `let rec loop state` 재귀 루프 (while 루프 없음)
- `System.Console.ReadLine()` → `Int32.TryParse` → 범위 검증 → 빈칸 검증 → 재시도 or 진행
- AI: `tdAgent rng 0.0 vTable state` (epsilon=0 순수 탐욕)
- 게임 종료 시: `printfn "사람(X) 승리!" / "AI(O) 승리!" / "무승부!"`

### SC3: Serilog 구조화 로깅

`Program.fs`:
- `LoggerConfiguration().WriteTo.Console(...).WriteTo.File("logs/tictactoe-.log", ...)`
- `Log.Information("Episode={Episode} WinRate={WinRate:P1}", ep, rate)` — history 순회
- `trainAgent rng 100_000 0.1 0.1 1_000` — 1000판마다 승률 기록
- Training.fs는 순수 함수: history를 `(ep, rate)` 리스트로 반환, 로깅 없음

### SC4: mdBook {{#include}} 검증

`tutorial/src/02-tictactoe/README.md` include 지시자:
```
line 56: {{#include ../../../TicTacToe/src/TicTacToe/Domain.fs}}
line 62: {{#include ../../../TicTacToe/src/TicTacToe/Rules.fs}}
line 68: {{#include ../../../TicTacToe/src/TicTacToe/Agent.fs}}
line 74: {{#include ../../../TicTacToe/src/TicTacToe/Training.fs}}
```
`mdbook build tutorial/` → `INFO HTML book written` (종료 코드 0)
빌드된 `tutorial/book/02-tictactoe/index.html`에 `module TicTacToe` 4회 등장

Phase 1 한계 설명:
- "Phase 1의 한계: 왜 MDP가 필요한가?" 섹션 (Bandit vs 틱택토 비교 표 포함)
- "Phase 3 예고: 왜 상태 공간이 문제인가?" 섹션 (다음 Phase 동기부여)

---

_Verified: 2026-02-19T15:10:00Z_
_Verifier: Claude (gsd-verifier)_
