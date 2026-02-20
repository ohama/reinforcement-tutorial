module ConnectFour.Console.Training

open ConnectFour.Domain
open ConnectFour.Rules
open ConnectFour.QAgent

type EpisodeOutcome = RedWon | YellowWon | Drew

type TrainingResult = {
    RedTable:       QTable
    YellowTable:    QTable
    TotalEpisodes:  int
    History:        (int * int * float) list
}

let playEpisode
    (rng: System.Random) (redTable: QTable) (yellowTable: QTable)
    (alpha: float) (gamma: float) (epsilon: float) : EpisodeOutcome =
    let rec loop (board: Board) (currentPlayer: Cell) =
        match isGameOver board with
        | Some RedWins    -> RedWon
        | Some YellowWins -> YellowWon
        | Some Draw       -> Drew
        | None ->
            let table = if currentPlayer = Red then redTable else yellowTable
            let state = encodeState board
            let action = chooseAction rng table board epsilon
            let nextBoard = applyMove board currentPlayer action
            match isGameOver nextBoard with
            | Some RedWins ->
                let reward = if currentPlayer = Red then RewardWin else RewardLoss
                updateQ table state action reward (encodeState nextBoard) [] alpha gamma true
                if currentPlayer = Red then RedWon else YellowWon
            | Some YellowWins ->
                let reward = if currentPlayer = Yellow then RewardWin else RewardLoss
                updateQ table state action reward (encodeState nextBoard) [] alpha gamma true
                if currentPlayer = Yellow then YellowWon else RedWon
            | Some Draw ->
                updateQ table state action RewardDraw (encodeState nextBoard) [] alpha gamma true
                Drew
            | None ->
                let nextState = encodeState nextBoard
                let nextLegal = legalMoves nextBoard
                updateQ table state action RewardStep nextState nextLegal alpha gamma false
                loop nextBoard (opponent currentPlayer)
    loop (emptyBoard ()) Red

let trainQLearning
    (rng: System.Random) (episodes: int) (alpha: float) (gamma: float)
    (epsilonStart: float) (epsilonEnd: float) (logInterval: int) : TrainingResult =
    let redTable = createQTable ()
    let yellowTable = createQTable ()
    let history = System.Collections.Generic.List<int * int * float>()
    let epsilonDecay = (epsilonStart - epsilonEnd) / float episodes
    let mutable redWins = 0
    let mutable totalGames = 0
    for ep in 1 .. episodes do
        let epsilon = max epsilonEnd (epsilonStart - epsilonDecay * float ep)
        let outcome = playEpisode rng redTable yellowTable alpha gamma epsilon
        totalGames <- totalGames + 1
        match outcome with RedWon -> redWins <- redWins + 1 | _ -> ()
        if ep % logInterval = 0 then
            let redWinRate = float redWins / float totalGames
            history.Add(ep, redTable.Count, redWinRate)
    { RedTable = redTable; YellowTable = yellowTable; TotalEpisodes = episodes
      History = history |> Seq.toList }

let playQAgentVsRandom
    (rng: System.Random) (table: QTable) (agentPlayer: Cell) : Cell option * int =
    let rec loop board currentPlayer moves =
        match isGameOver board with
        | Some RedWins    -> Some Red, moves
        | Some YellowWins -> Some Yellow, moves
        | Some Draw       -> None, moves
        | None ->
            let col =
                if currentPlayer = agentPlayer then chooseAction rng table board 0.0
                else let legal = legalMoves board in legal.[rng.Next(legal.Length)]
            loop (applyMove board currentPlayer col) (opponent currentPlayer) (moves + 1)
    loop (emptyBoard ()) Red 0
