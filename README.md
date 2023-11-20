# Variation-Chess
## Topic

Choose the theme of "variation four chess", and the teacher discussed through.

Rules: On the 8*8 board, the two sides play in order, one side in the vertical, vertical, two diagonal lines into 4 to win. Each side has two opportunities for "alienation", that is, to lay its own pieces on the other side's existing positions, and to nullify the original positions of the pieces.

Description: Each walk must be within 60 seconds, the right side has timing. The right side win ratio is the value ratio judgment of the two pieces evaluated by the system.



## Run

Run ./netcoreapp3.1 /Chess. exe directly

The source code is in MainWindow.xaml.cs, and the interface design is in MainWindow.xaml

The task implementation selects a niche C#+XAML implementation, and all the code does not transcribe any published code. The effect of XAML controls is really small, far less than python libraries.

Since the algorithm has a certain randomness, please run it several times to see the effect, and the first-hand advantage of the game rules setting is very large, the first few hands had better put some water...





## Algorithm description

In this paper, two search algorithms are written and compared.



1. Minimax algorithm with alpha-beta pruning

Principle [Minimax()] : Each call recursively searches the decision tree, and each node is a simulated situation. In the maximization layer, the algorithm selects the move that can make the value of the evaluation function maximum; In the minimization layer, the algorithm selects the move that minimizes the value of the evaluation function, which uses maximizingPlayer to rotate between the maximum and minimization layers. The recursive dive ends at a predetermined depth.

As it traverses the child nodes, the function uses alpha-beta pruning to optimize the search. If the evaluation value at a child node exceeds beta (for the minimization layer) or falls below alpha (for the maximization layer), the function terminates its search early and stops traversing the other child nodes.

evaluate function [evaluate()] : Use the four functions to calculate the position value of the four directions. Specifically, the more chess pieces of the same color in the same direction, the value of the whole position will increase exponentially with PLUS.



Analysis: Minimax() searches alternately between maximized and minimized nodes, and there is pruning, which may cause even a child node with a very high score to be ignored, because under minimized nodes, the algorithm preferrals the child node with a low score. From the point of view of the operation effect, there may be some "even three" situations, that is, when you or your opponent has played three consecutive pieces and is about to win, the algorithm does not necessarily give priority to win or block. Of course, this is also limited by the recursive depth. I try to find that in my environment, the SEARCH_DEPTH of 4 is close to the limit. If there is a deeper search, the value of Minimax can be better reflected.



2. Greedy algorithm

Principle [GreedyMove()] : Based on the limitations of the Minimax algorithm, I wrote a greedy algorithm that iterates to get the highest value of the current dot move. Use the same evaluation function evaluate().



Analysis: For such a small scale problem, traversing all nodes seems to be better than searching Minimax with limited depth; On a larger chessboard, however, it is impossible to traverse all possible nodes in this way, and the greedy algorithm only considers its own current value without considering the value of the opponent from a game perspective. From the point of view of the operation effect, GreedyMove gives priority to the position of increasing its own chess, but it does not consider the opponent's containment of its own, which will lead to many seemingly high value chess shapes being destroyed by the opponent's containment.
