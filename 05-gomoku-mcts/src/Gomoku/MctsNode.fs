module Gomoku.MctsNode

open System.Collections.Generic

/// Mutable MCTS tree node. MUST be a class (not record) for parent pointers + mutable children dict.
/// Prior is mutable so Dirichlet noise can be applied to root children (Plan 03).
type MctsNode(parent: MctsNode option, prior: float) =
    let children = Dictionary<int, MctsNode>()
    let mutable visits     = 0
    let mutable totalValue = 0.0
    let mutable isExpanded = false
    let mutable prior_     = prior   // mutable for Dirichlet noise

    member _.Parent      = parent
    member _.Children    = children
    member _.Visits      = visits
    member _.TotalValue  = totalValue
    member _.IsExpanded  = isExpanded
    member _.Prior       with get() = prior_ and set(v) = prior_ <- v

    /// Q-value: average value seen through this node.
    member _.Q () =
        if visits = 0 then 0.0
        else totalValue / float visits

    /// Expand this node with (action, prior_probability) pairs.
    member this.Expand(actionPriors: (int * float) seq) =
        for (action, p) in actionPriors do
            if not (children.ContainsKey(action)) then
                children.[action] <- MctsNode(Some this, p)
        isExpanded <- true

    /// Update this node with a new value observation.
    member _.Update(value: float) =
        visits     <- visits + 1
        totalValue <- totalValue + value

    /// Backpropagate value up the tree, negating at each level.
    /// Call as: leaf.UpdateRecursive(-leafValue)
    /// Convention: value is from the perspective of the player who just MOVED (opposite of node's current player).
    /// Negation at each parent = perspective flip for zero-sum game.
    member this.UpdateRecursive(value: float) =
        this.Update(value)
        match parent with
        | Some p -> p.UpdateRecursive(-value)
        | None   -> ()

    member _.IsLeaf () = children.Count = 0
