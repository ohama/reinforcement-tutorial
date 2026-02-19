# Chapter 1: 슬롯머신 — 탐색과 활용

## 핵심 개념

**Multi-Armed Bandit 문제**: N개의 슬롯머신(arm) 중 어느 것을 당길지 선택하는 문제.

**탐색 vs 활용 (Exploration vs Exploitation)**:
- **탐색**: 새로운 arm을 시도해 더 나은 선택지를 발견
- **활용**: 현재 알고 있는 최선의 arm을 선택

## 핵심 F# 타입

```fsharp
type Arm = int

type AgentState = {
    Counts: int array
    Values: float array
}

type BanditEnv = {
    RewardProbs: float array
}
```

## 알고리즘

(상세 내용은 Plan 03에서 작성)

### ε-greedy

확률 ε로 무작위 탐색, 확률 (1-ε)로 현재 최선의 arm 선택.

### UCB1

불확실성이 높은 arm에 탐색 보너스를 부여해 결정론적으로 탐색과 활용을 균형 잡음.
