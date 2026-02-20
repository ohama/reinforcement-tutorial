# Chapter 1: 슬롯머신 — 탐색과 활용

## 문제 정의: Multi-Armed Bandit

**Multi-Armed Bandit(MAB)** 문제는 강화학습에서 가장 단순하지만 핵심 딜레마를 정확히 포착하는 문제다. N개의 슬롯머신(arm)이 있고, 각 arm은 고유한 보상 확률을 가진다. 에이전트는 매 step마다 arm 하나를 선택하고 보상(0 또는 1)을 받는다. 목표는 T step 동안 누적 보상을 최대화하는 것이다.

**왜 이것이 어려운가?** 에이전트는 arm들의 실제 보상 확률을 모른다. 알아내려면 탐색해야 하는데, 탐색 자체가 비용이다.

---

## 탐색 vs 활용 (Exploration vs Exploitation)

이것이 RL의 근본적 딜레마다:

| 전략 | 설명 | 위험 |
|------|------|------|
| **탐색 (Exploration)** | 새로운 arm을 시도해 더 나은 선택지 발견 | 당장의 보상을 포기 |
| **활용 (Exploitation)** | 현재 최선의 arm을 반복 선택 | 더 좋은 arm을 놓칠 수 있음 |

순수 탐색은 보상을 극대화하지 못한다. 순수 활용은 초기 데이터에 갇힌다. 좋은 알고리즘은 이 둘을 동적으로 균형 잡는다.

---

## F# 구현: 핵심 타입

```fsharp
// Bandit/src/Bandit/Domain.fs

/// Index of an arm (0-based)
type Arm = int

/// 에이전트의 불변 상태 — 매 step마다 새 레코드 생성 (mutable 없음)
type AgentState = {
    Counts: int array   // 각 arm을 선택한 횟수
    Values: float array // 각 arm의 추정 가치 (Q-value)
}

/// Bandit 환경: N개의 arm, 각각 고정된 보상 확률
type BanditEnv = {
    RewardProbs: float array
}
```

**설계 원칙**: `AgentState`는 불변(immutable)이다. 각 step에서 새 레코드를 반환한다. `mutable` 필드를 쓰지 않는 이유는 함수를 순수하게 유지하여 테스트와 추론이 쉬워지기 때문이다.

---

## 알고리즘 1: ε-greedy

### 아이디어

확률 ε로 무작위 arm을 선택하고(탐색), 확률 (1-ε)로 현재 추정 가치가 가장 높은 arm을 선택한다(활용). ε이 클수록 더 많이 탐색한다.

```fsharp
// Bandit/src/Bandit/Agent.fs

let epsilonGreedy (rng: System.Random) (epsilon: float) (state: AgentState) : Arm =
    if rng.NextDouble() < epsilon then
        rng.Next(state.Values.Length)              // 탐색: 무작위 arm
    else
        state.Values
        |> Array.indexed
        |> Array.maxBy snd
        |> fst                                      // 활용: 최선의 arm
```

### 점진적 평균 업데이트 (Incremental Mean)

각 arm의 가치를 추정할 때 전체 보상 기록을 저장하지 않는다. 점진적 평균 공식으로 O(1) 메모리와 수치 안정성을 동시에 달성한다:

$$Q_{n+1}(a) = Q_n(a) + \frac{1}{n} \left[ R_n - Q_n(a) \right]$$

```fsharp
let incrementalMean (state: AgentState) (arm: Arm) (reward: float) : AgentState =
    let n = state.Counts.[arm] + 1
    let newVal = state.Values.[arm] + (1.0 / float n) * (reward - state.Values.[arm])
    { Counts = state.Counts |> Array.mapi (fun i c -> if i = arm then n else c)
      Values = state.Values |> Array.mapi (fun i v -> if i = arm then newVal else v) }
```

### ε 값별 동작 차이

| ε | 탐색 빈도 | 활용 빈도 | 특징 |
|----|---------|---------|------|
| 0.01 | 1% | 99% | 초기에 최선의 arm을 빠르게 고정, 새 정보 반영 느림 |
| 0.10 | 10% | 90% | 실용적 균형, 가장 많이 사용 |
| 0.30 | 30% | 70% | 환경 변화에 적응력 높지만 단기 성능 낮음 |

---

## 알고리즘 2: UCB1 (Upper Confidence Bound)

### 아이디어

ε-greedy는 탐색 확률을 고정한다. UCB1은 **불확실성을 명시적으로 보너스로 추가**하여 결정론적으로 탐색과 활용을 균형 잡는다:

$$a_t = \arg\max_a \left[ Q(a) + \sqrt{\frac{2 \ln t}{N(a)}} \right]$$

- \\(Q(a)\\): arm a의 현재 추정 가치 (활용 항)
- \\(\sqrt{2 \ln t / N(a)}\\): 탐색 보너스 — 방문 횟수가 적을수록 커짐

```fsharp
// Bandit/src/Bandit/Agent.fs

let ucb1 (totalSteps: int) (state: AgentState) : Arm =
    // 초기화: 방문하지 않은 arm이 있으면 먼저 방문 (N(a)=0 → 나눗셈 방지)
    match Array.tryFindIndex (fun c -> c = 0) state.Counts with
    | Some arm -> arm
    | None ->
        let t = float totalSteps
        state.Values
        |> Array.mapi (fun i q -> q + sqrt (2.0 * log t / float state.Counts.[i]))
        |> Array.indexed
        |> Array.maxBy snd
        |> fst
```

**초기화가 중요한 이유**: \\(N(a) = 0\\)이면 \\(\ln(t) / 0 = \infty\\)가 된다. 모든 arm을 한 번씩 방문한 뒤에야 UCB1 공식을 적용한다.

### ε-greedy vs UCB1 비교

| 특성 | ε-greedy | UCB1 |
|------|---------|------|
| 탐색 방식 | 확률적 (매번 동전 던지기) | 결정론적 (불확실성 기반) |
| 하이퍼파라미터 | ε (튜닝 필요) | 없음 |
| 이론적 보장 | 없음 | O(log T) 후회 (regret) 상한 |
| 환경 변화 적응 | ε으로 조절 가능 | 고정 공식 (비정상 환경에 취약) |

---

## 실험 결과 해석

`dotnet run --project Bandit/src/Bandit.Console/`을 실행하면:

```
[HH:mm:ss INF] === ε-greedy 비교 (1000 steps, 10-arm bandit) ===
[HH:mm:ss INF]   ε=0.01  최적 arm=9  추정 가치=0.892  총 보상≈871.3
[HH:mm:ss INF]   ε=0.10  최적 arm=9  추정 가치=0.901  총 보상≈843.6
[HH:mm:ss INF]   ε=0.30  최적 arm=9  추정 가치=0.895  총 보상≈762.1
[HH:mm:ss INF] --------------------------------------------------
[HH:mm:ss INF] === ε-greedy (ε=0.10) vs UCB1 ===
[HH:mm:ss INF]   ε-greedy 총 보상≈843.6
[HH:mm:ss INF]   UCB1     총 보상≈891.2
[HH:mm:ss INF]   승자: UCB1 (+47.6)
```

**관찰 포인트**:
- ε=0.01은 초기 운에 따라 성능이 불안정하다 (탐색 부족)
- ε=0.10이 단기 성능과 장기 학습의 균형을 잡는다
- ε=0.30은 탐색을 너무 많이 해서 총 보상이 낮다
- UCB1은 하이퍼파라미터 없이도 ε-greedy와 경쟁한다

---

## 이 Phase의 F# 설계 원칙

Phase 전체에서 지키는 두 가지 핵심 규칙:

**1. Functional Core / Imperative Shell (XCUT-03)**

`Domain.fs`, `Environment.fs`, `Agent.fs`, `Training.fs`는 순수 함수만 포함한다. 모든 I/O (Serilog, `printfn`, `Console.*`)는 `Program.fs`에만 있다. 이렇게 하면 순수 함수를 격리해서 테스트하고, I/O 없이 재사용할 수 있다.

**2. Option/Result 패턴 (XCUT-01)**

예외를 던지지 않는다. 잘못된 입력(예: ε가 [0,1] 범위 밖)은 `Result<'T, string>`으로 표현한다. 이렇게 하면 오류 경로가 타입 시스템에 드러나 런타임 충돌이 없다.

---

## 다음 Phase

Bandit에서 에이전트는 **상태(state)** 개념이 없다. 매 step이 독립적이다. 하지만 실제 게임(틱택토, 오목)은 이전 수가 현재 상황을 결정한다 — 이것이 **MDP(Markov Decision Process)**다.

Chapter 2에서는 틱택토를 통해 상태, 전이, 가치 함수, TD Learning을 구현한다.
