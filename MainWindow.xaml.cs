using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Effects;


namespace Chess
{
    // NodeTypes of Minimax Tree
    public enum nodetype { max, min };

    // PieceTypes
    public enum piecetype { black, white };

    public partial class MainWindow : Window
    {
        private const int WINNING_LENGTH = 4; // 4 pieces to win
        private const int ROWS = 8;
        private const int COLS = 8;
        private const int LBOUND = 0;// left bound
        private const int RBOUND = 7;// right bound
        private const int UBOUND = 0;// up(top) bound
        private const int DBOUND = 7;// down(bottom) bound
        private const int PLUS = 1000;// value multiplication
        private const int SEARCH_DEPTH = 3;
        private const int VARIATION = 2;// chances of variation
        private int[,] POSITIONVALUE = // indicate the value of position
        {
            {0,0,0,0,0,0,0,0 },
            {0,PLUS,PLUS,PLUS,PLUS,PLUS,PLUS,0 },
            {0,PLUS,PLUS*3,PLUS*3,PLUS*3,PLUS*3,PLUS,0 },
            {0,PLUS,PLUS*3,PLUS*5,PLUS*5,PLUS*3,PLUS,0 },
            {0,PLUS,PLUS*3,PLUS*5,PLUS*5,PLUS*3,PLUS,0 },
            {0,PLUS,PLUS*3,PLUS*3,PLUS*3,PLUS*3,PLUS,0 },
            {0,PLUS,PLUS,PLUS,PLUS,PLUS,PLUS,0 },
            {0,0,0,0,0,0,0,0 },
        };
          
        private List<Position> BlackEllipsePieces;
        private List<Position> WhiteEllipsePieces;
        protected int my_variation, ai_variation;
        public char[,] Distribution;// a map to record the chessboard distribution

        public MainWindow()
        {
            InitializeComponent();

            BlackEllipsePieces = new List<Position>();
            WhiteEllipsePieces = new List<Position>();
            Distribution = new char[ROWS, COLS];
            my_variation = VARIATION;
            ai_variation = VARIATION;

            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    Distribution[i, j] = 'n';
                }
            }
            MainWindow_Loaded();
        }

        private async void MainWindow_Loaded()
        {
            await Running();
        }

        // main program
        public async Task Running()
        {
            // set count-down for player
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            int remainingTime = 60;

            timer.Tick += (sender, e) =>
            {
                remainingTime--;
                CountDown.Text = "Remain "+remainingTime.ToString()+" second(s)";

                if (remainingTime <= 0)
                {
                    timer.Stop();
                    // 超时，退出Running()方法
                    return;
                }
            };

            while (true)
            {
                Outcome.Text = "Your latest winning rate        " + (CalculateWinRate() * 100).ToString("F2") + "%.";

                remainingTime = 60;
                CountDown.Text = "Remain " + remainingTime.ToString() + " second(s)";
                timer.Start();

                await PlayerMove();

                timer.Stop();

                if (IsWin(piecetype.black))
                {
                    Outcome.Text = "Black wins!";
                    break;
                }

                if (IsDraw())
                {
                    Outcome.Text = "Draw!";
                    break;
                }

                ComputerMove();
                //GreedyMove();

                if (IsWin(piecetype.white))
                {
                    Outcome.Text = "White wins!";
                    break;
                }
        
                if (IsDraw())
                {
                    Outcome.Text = "Draw!";
                    break;
                }
            }
}

        private Task<Position> WaitForMouseClick()
        {
            var tcs = new TaskCompletionSource<Position>();
            //tcs = null;
            MouseButtonEventHandler mouseLeftButtonDownHandler = null;

            mouseLeftButtonDownHandler = (sender, e) =>
            {
                Point clickPosition = e.GetPosition(chessboard);
                int row = (int)(clickPosition.Y / (chessboard.ActualHeight / ROWS));
                int col = (int)(clickPosition.X / (chessboard.ActualWidth / COLS));

                tcs.SetResult(new Position(row, col));
                chessboard.MouseLeftButtonDown -= mouseLeftButtonDownHandler;
            };

            // 绑定事件处理程序
            chessboard.MouseLeftButtonDown += mouseLeftButtonDownHandler;

            return tcs.Task;
        }

        // record the move of player
        private async Task PlayerMove()
        {
            while (true)
            {
                Position check = await WaitForMouseClick();

                if (IsValidMove(check,piecetype.black))
                {
                    chessboard.Dispatcher.Invoke(() =>
                    {   if(Distribution[check.getx(),check.gety()]=='n')
                        {
                            Ellipse NewBlack = CreateEllipse(30, 30, Brushes.Black);
                            AddEllipseToChessboard(NewBlack, check, false);
                            Distribution[check.getx(), check.gety()] = 'b';
                        }
                        else if(Distribution[check.getx(), check.gety()] == 'w')
                        {
                            my_variation--;
                            Ellipse NewBlack = CreateEllipse(30, 30, Brushes.Black);
                            AddEllipseToChessboard(NewBlack, check, true);
                            Distribution[check.getx(), check.gety()] = 'b';
                            WhiteEllipsePieces.RemoveAll(p => p.getx() == check.getx() && p.gety() == check.gety());
                        }
                    });
                    BlackEllipsePieces.Add(check);              
                    break;
                };
            }
        }

        private List<Position> GetValidMoves(piecetype type)
        {
            List<Position> validMoves = new List<Position>();

            int lb = LBOUND, rb = RBOUND, ub = UBOUND, db = DBOUND;

            for (int i = lb; i <= rb; i++)
            {
                for (int j = ub; j <= db; j++)
                {
                    if (IsValidMove(i, j, type))
                    {
                        validMoves.Add(new Position(i, j));
                    }
                }
            }
            return validMoves;
        }

        // minimax algorithm
        private double Minimax(node currentNode, int depth, double alpha, double beta, bool maximizingPlayer)
        {
            if (depth == 0 || IsWin(piecetype.white) || IsWin(piecetype.black))
            {
                return currentNode.value;
            }

            if (maximizingPlayer)
            {
                double maxEval = double.NegativeInfinity;
                foreach (Position move in GetValidMoves(piecetype.white))
                {
                    WhiteEllipsePieces.Add(move);
                    double eval = Minimax(new node(depth - 1, evaluate(move), alpha, beta, nodetype.min), depth - 1, alpha, beta, false);
                    WhiteEllipsePieces.Remove(move);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                return maxEval;
            }
            else
            {
                double minEval = double.PositiveInfinity;
                foreach (Position move in GetValidMoves(piecetype.white))
                {
                    BlackEllipsePieces.Add(move);
                    double eval = Minimax(new node(depth - 1, evaluate(move), alpha, beta, nodetype.max), depth - 1, alpha, beta, true);
                    BlackEllipsePieces.Remove(move);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                return minEval;
            }
        }

        //Greedy algorithm
        private void GreedyMove()
        {
            double bestScore = double.NegativeInfinity;
            Position bestMove = null;

            foreach (Position move in GetValidMoves(piecetype.white))
            {
                WhiteEllipsePieces.Add(move);
                double score = evaluate(move);
                WhiteEllipsePieces.Remove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            if (bestMove != null)
            {
                if (Distribution[bestMove.getx(), bestMove.gety()] == 'n')
                {
                    Ellipse NewWhite = CreateEllipse(30, 30, Brushes.White);
                    AddEllipseToChessboard(NewWhite, bestMove, false);
                    Distribution[bestMove.getx(), bestMove.gety()] = 'w';
                }
                else if (Distribution[bestMove.getx(), bestMove.gety()] == 'b')
                {
                    ai_variation--;
                    Ellipse NewWhite = CreateEllipse(30, 30, Brushes.White);
                    AddEllipseToChessboard(NewWhite, bestMove, true);
                    Distribution[bestMove.getx(), bestMove.gety()] = 'w';
                    BlackEllipsePieces.RemoveAll(p => p.getx() == bestMove.getx() && p.gety() == bestMove.gety());
                }
                WhiteEllipsePieces.Add(bestMove);
            }
            else try
                {
                    throw new Exception("Searching error.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Environment.Exit(-2);
                }
        }

        // an attempt, a hybrid algorithm of minimax and greedy
        private double HybridMove(node currentNode, int depth, double alpha, double beta, bool maximizingPlayer)
        {
            if (depth == 0 || IsWin(piecetype.white) || IsWin(piecetype.black))
            {
                return currentNode.value;
            }

            if (depth <= 2) // Use Greedy algorithm when depth is less than or equal to 2
            {
                double bestScore = maximizingPlayer ? double.NegativeInfinity : double.PositiveInfinity;
                Position bestMove = null;

                foreach (Position move in GetValidMoves(maximizingPlayer ? piecetype.white : piecetype.black))
                {
                    double score = evaluate(move);

                    if (maximizingPlayer && score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    else if (!maximizingPlayer && score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }

                return bestScore;
            }
            else // Use Minimax algorithm when depth is greater than 2
            {
                if (maximizingPlayer)
                {
                    double maxEval = double.NegativeInfinity;
                    foreach (Position move in GetValidMoves(piecetype.white))
                    {
                        double eval = HybridMove(new node(depth - 1, evaluate(move), alpha, beta, nodetype.min), depth - 1, alpha, beta, false);
                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                    return maxEval;
                }
                else
                {
                    double minEval = double.PositiveInfinity;
                    foreach (Position move in GetValidMoves(piecetype.black))
                    {
                        double eval = HybridMove(new node(depth - 1, evaluate(move), alpha, beta, nodetype.max), depth - 1, alpha, beta, true);
                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                    return minEval;
                }
            }
        }

        // a test, a random algorithm
        private void RComputerMove()
        {
            int x, y;
            while (true)
            {
                Random random = new Random();
                x = random.Next(0, ROWS);
                y = random.Next(0, COLS);
                if (IsValidMove(x, y, piecetype.white))
                {
                    Ellipse NewWhite = CreateEllipse(30, 30, Brushes.White);
                    AddEllipseToChessboard(NewWhite, x, y, false);
                    WhiteEllipsePieces.Add(new Position(x, y));
                    Distribution[x, y] = 'w';
                    break;
                }
            }
        }

        // record the move of computer
        private void ComputerMove()
        {
            double bestScore = double.NegativeInfinity;
            Position bestMove = null;
            List<Position> bestMoves = new List<Position>();

            if (GetAllCount() == 1)
            {
                Position firstblack = BlackEllipsePieces[0];
                int firstblackx = firstblack.getx();
                int firstblacky = firstblack.gety();
                int firstwhitex = firstblackx > 3 ? firstblackx - 1 : firstblackx + 1;
                int firstwhitey = firstblacky > 3 ? firstblacky - 1 : firstblacky + 1;
                bestMove = new Position(firstwhitex, firstwhitey);
            }
            else
            {
                foreach (Position move in GetValidMoves(piecetype.white))
                {
                    WhiteEllipsePieces.Add(move);
                    double score = Minimax(new node(SEARCH_DEPTH, evaluate(move), double.NegativeInfinity, double.PositiveInfinity, nodetype.min), SEARCH_DEPTH, double.NegativeInfinity, double.PositiveInfinity, false);
                    WhiteEllipsePieces.Remove(move);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoves.Clear(); // Clear the list of best moves
                        bestMoves.Add(move); // Add the current move to the list
                    }
                    else if (score == bestScore)
                    {
                        bestMoves.Add(move); // Add the current move to the list
                    }
                }
            }

            if (bestMoves.Count > 0)
            {
                int randomIndex = new Random().Next(0, bestMoves.Count);
                bestMove = bestMoves[randomIndex];
            }

            if (bestMove != null)
            {
                if (Distribution[bestMove.getx(), bestMove.gety()] == 'n')
                {
                    Ellipse NewWhite = CreateEllipse(30, 30, Brushes.White);
                    AddEllipseToChessboard(NewWhite, bestMove, false);
                    Distribution[bestMove.getx(), bestMove.gety()] = 'w';
                }
                else if (Distribution[bestMove.getx(), bestMove.gety()] == 'b')
                {
                    ai_variation--;
                    Ellipse NewWhite = CreateEllipse(30, 30, Brushes.White);
                    AddEllipseToChessboard(NewWhite, bestMove, true);
                    Distribution[bestMove.getx(), bestMove.gety()] = 'w';
                    BlackEllipsePieces.RemoveAll(p => p.getx() == bestMove.getx() && p.gety() == bestMove.gety());
                }
                WhiteEllipsePieces.Add(bestMove);               
            }
            else try
                {
                    throw new Exception("Searching error.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Environment.Exit(-2);
                }
        }

        // evaluate function
        public int evaluate(Position p)
        {
            int value = 0;
            int x = p.getx();
            int y = p.gety();

            //Horizontal
            value += HorizontalValue(x, y);

            //Vertical
            value += VerticalValue(x, y);

            //Lean_Left
            value += LeftCrossValue(x, y);

            //Lean_Right
            value += RightCrossValue(x, y);

            value += POSITIONVALUE[p.getx(), p.gety()];
            return value;
        }

        private int HorizontalValue(int x, int y)
        {
            int score = 0;
            char rcheck = x >= RBOUND ? 'n' : Distribution[x + 1, y];
            char lcheck = x <= LBOUND ? 'n' : Distribution[x - 1, y];
            int flag1 = 1, flag2 = 1, flag3 = 1, flag4 = 1, flag5 = 1;

            if (rcheck == 'n' && lcheck == 'n') return score;

            if(rcheck == 'n')
            {
                for(int i=x-1;i>x-WINNING_LENGTH;i--)
                {
                    if (i <= LBOUND) return score;
                    else
                    {
                        if (Distribution[i, y] == lcheck || Distribution[i, y] == 's')
                        {
                            score += flag1;
                            flag1 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (lcheck == 'n')
            {
                for (int i = x + 1; i < x + WINNING_LENGTH; i++)
                {
                    if (i >= RBOUND) return score;
                    else
                    {
                        if (Distribution[i, y] == rcheck || Distribution[i, y] == 's')
                        {
                            score += flag2;
                            flag2 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if(lcheck == rcheck || lcheck == 's' || rcheck == 's')
            {
                for (int i = x - 1; i > x - WINNING_LENGTH; i--)
                {
                    if (i <= LBOUND) break;
                    else
                    {
                        if (Distribution[i, y] == lcheck || Distribution[i, y] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = x + 1; i < x + WINNING_LENGTH; i++)
                {
                    if (i >= RBOUND) return score;
                    else
                    {
                        if (Distribution[i, y] == rcheck || Distribution[i, y] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if((lcheck == 'b' && rcheck == 'w') || (lcheck == 'w' && rcheck == 'b'))
            {
                for (int i = x - 1; i > x - WINNING_LENGTH; i--)
                {
                    if (i <= LBOUND) break;
                    else
                    {
                        if (Distribution[i, y] == lcheck || Distribution[i, y] == 's')
                        {
                            score += flag4;
                            flag4 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = x + 1; i < x + WINNING_LENGTH; i++)
                {
                    if (i >= RBOUND) return score;
                    else
                    {
                        if (Distribution[i, y] == rcheck || Distribution[i, y] == 's')
                        {
                            score += flag5;
                            flag5 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            return score;
        }

        private int VerticalValue(int x, int y)
        {
            int score = 0;
            char dcheck = y >= DBOUND ? 'n' : Distribution[x , y + 1];
            char ucheck = y <= UBOUND ? 'n' : Distribution[x, y - 1];
            int flag1 = 1, flag2 = 1, flag3 = 1, flag4 = 1, flag5 = 1;

            if (ucheck == 'n' && dcheck == 'n') return score;

            if (dcheck == 'n')
            {
                for (int i = y - 1; i > y - WINNING_LENGTH; i--)
                {
                    if (i <= UBOUND) return score;
                    else
                    {
                        if (Distribution[x, i] == ucheck || Distribution[x, i] == 's')
                        {
                            score += flag1;
                            flag1 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (ucheck == 'n')
            {
                for (int i = y + 1; i < y + WINNING_LENGTH; i++)
                {
                    if (i >= DBOUND) return score;
                    else
                    {
                        if (Distribution[x, i] == dcheck || Distribution[x, i] == 's')
                        {
                            score += flag2;
                            flag2 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (ucheck == dcheck || ucheck == 's' || dcheck == 's')
            {
                for (int i = y - 1; i > y - WINNING_LENGTH; i--)
                {
                    if (i <= UBOUND) break;
                    else
                    {
                        if (Distribution[x, i] == ucheck || Distribution[x, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y + 1; i < y + WINNING_LENGTH; i++)
                {
                    if (i >= DBOUND) return score;
                    else
                    {
                        if (Distribution[x, i] == dcheck || Distribution[x, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if ((ucheck == 'b' && dcheck == 'w') || (ucheck == 'w' && dcheck == 'b'))
            {
                for (int i = y - 1; i > y - WINNING_LENGTH; i--)
                {
                    if (i <= UBOUND) break;
                    else
                    {
                        if (Distribution[x, i] == ucheck || Distribution[x, i] == 's')
                        {
                            score += flag4;
                            flag4 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y + 1; i < y + WINNING_LENGTH; i++)
                {
                    if (i >= DBOUND) return score;
                    else
                    {
                        if (Distribution[x, i] == dcheck || Distribution[x, i] == 's')
                        {
                            score += flag5;
                            flag5 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            return score;
        }

        // lean-left
        private int LeftCrossValue(int x,int y)
        {
            int score = 0;
            char rucheck = x >= RBOUND || y <= UBOUND ? 'n' : Distribution[x + 1, y - 1];
            char ldcheck = x <= LBOUND || y >= DBOUND ? 'n' : Distribution[x - 1, y + 1];
            int flag1 = 1, flag2 = 1, flag3 = 1, flag4 = 1, flag5 = 1;

            if (rucheck == 'n' && ldcheck == 'n') return score;
            
            if (ldcheck == 'n')
            {
                for (int i = y - 1,j=x+1; i > y - WINNING_LENGTH; i--,j++)
                {
                    if (i <= UBOUND || j >= RBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == rucheck || Distribution[j, i] == 's')
                        {
                            score += flag1;
                            flag1 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (rucheck == 'n')
            {
                for (int i = y + 1,j = x - 1; i < y + WINNING_LENGTH; i++,j--)
                {
                    if (i >= DBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == ldcheck || Distribution[j, i] == 's')
                        {
                            score += flag2;
                            flag2 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (rucheck == ldcheck || rucheck == 's' || ldcheck == 's')
            {
                for (int i = y - 1, j = x + 1; i > y - WINNING_LENGTH; i--, j++)
                {
                    if (i <= UBOUND || j >= RBOUND) break;
                    else
                    {
                        if (Distribution[j, i] == rucheck || Distribution[j, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y + 1, j = x - 1; i < y + WINNING_LENGTH; i++, j--)
                {
                    if (i >= DBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == ldcheck || Distribution[j, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if ((rucheck == 'b' && ldcheck == 'w') || (rucheck == 'w' && ldcheck == 'b'))
            {
                for (int i = y - 1, j = x + 1; i > y - WINNING_LENGTH; i--, j++)
                {
                    if (i <= UBOUND || j >= RBOUND) break;
                    else
                    {
                        if (Distribution[j, i] == rucheck || Distribution[j, i] == 's')
                        {
                            score += flag4;
                            flag4 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y + 1, j = x - 1; i < y + WINNING_LENGTH; i++, j--)
                {
                    if (i >= DBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == ldcheck || Distribution[j, i] == 's')
                        {
                            score += flag5;
                            flag5 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            return score;
        }

        // lean-right
        private int RightCrossValue(int x, int y)
        {
            int score = 0;
            char lucheck = x <= LBOUND || y <= UBOUND ? 'n' : Distribution[x - 1, y - 1];
            char rdcheck = x >= RBOUND || y >= DBOUND ? 'n' : Distribution[x + 1, y + 1];
            int flag1 = 1, flag2 = 1, flag3 = 1, flag4 = 1, flag5 = 1;

            if (lucheck == 'n' && rdcheck == 'n') return score;

            if (lucheck == 'n')
            {
                for (int i = y + 1, j = x + 1; i < y + WINNING_LENGTH; i++, j++)
                {
                    if (i >= DBOUND || j >= RBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == rdcheck || Distribution[j, i] == 's')
                        {
                            score += flag1;
                            flag1 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (rdcheck == 'n')
            {
                for (int i = y - 1, j = x - 1; i > y - WINNING_LENGTH; i--, j--)
                {
                    if (i <= UBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == lucheck || Distribution[j, i] == 's')
                        {
                            score += flag2;
                            flag2 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if (rdcheck == lucheck || rdcheck == 's' || lucheck == 's')
            {
                for (int i = y + 1, j = x + 1; i < y + WINNING_LENGTH; i++, j++)
                {
                    if (i >= DBOUND || j >= RBOUND) break;
                    else
                    {
                        if (Distribution[j, i] == rdcheck || Distribution[j, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y - 1, j = x - 1; i > y - WINNING_LENGTH; i--, j--)
                {
                    if (i <= UBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == lucheck || Distribution[j, i] == 's')
                        {
                            score += flag3;
                            flag3 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            if ((rdcheck == 'b' && lucheck == 'w') || (rdcheck == 'w' && lucheck == 'b'))
            {
                for (int i = y + 1, j = x + 1; i < y + WINNING_LENGTH; i++, j++)
                {
                    if (i >= DBOUND || j >= RBOUND) break;
                    else
                    {
                        if (Distribution[j, i] == rdcheck || Distribution[j, i] == 's')
                        {
                            score += flag4;
                            flag4 *= PLUS;
                        }
                        else break;
                    }
                }

                for (int i = y - 1, j = x - 1; i > y - WINNING_LENGTH; i--, j--)
                {
                    if (i <= UBOUND || j <= LBOUND) return score;
                    else
                    {
                        if (Distribution[j, i] == lucheck || Distribution[j, i] == 's')
                        {
                            score += flag5;
                            flag5 *= PLUS;
                        }
                        else return score;
                    }
                }
            }

            return score;
        }

        private bool IsValidMove(int row, int col, piecetype type)
        {
            if(type==piecetype.black&&my_variation>0)
            {
                return Distribution[row, col] != 'b' && Distribution[row, col] != 's';
            }

            if (type == piecetype.white && ai_variation > 0)
            {
                return Distribution[row, col] != 'w' && Distribution[row, col] != 's';
            }

            return Distribution[row, col] == 'n';
        }

        private bool IsValidMove(Position check, piecetype type)
        {
            if (type == piecetype.black && my_variation > 0)
            {
                return Distribution[check.getx(), check.gety()] != 'b' 
                    && Distribution[check.getx(), check.gety()] != 's';
            }

            if (type == piecetype.white && ai_variation > 0)
            {
                return Distribution[check.getx(), check.gety()] != 'w' 
                    && Distribution[check.getx(), check.gety()] != 's';
            }

            return Distribution[check.getx(), check.gety()] == 'n';
        }

        private bool IsWin(piecetype type)
        {
            ref List<Position> checkset = ref ((type == piecetype.white) ? ref WhiteEllipsePieces : ref BlackEllipsePieces);

            for (int i = 0; i < checkset.Count; i++)
            {
                int x = checkset[i].getx();
                int y = checkset[i].gety();

                // checked by Distribution
                if (checkset.Exists(p => p.getx() == x && p.gety() == y + 1) &&
                    checkset.Exists(p => p.getx() == x && p.gety() == y + 2) &&
                    checkset.Exists(p => p.getx() == x && p.gety() == y + 3))
                {
                    return true;
                }

                if (checkset.Exists(p => p.getx() == x + 1 && p.gety() == y) &&
                    checkset.Exists(p => p.getx() == x + 2 && p.gety() == y) &&
                    checkset.Exists(p => p.getx() == x + 3 && p.gety() == y))
                {
                    return true;
                }

                if (checkset.Exists(p => p.getx() == x + 1 && p.gety() == y + 1) &&
                    checkset.Exists(p => p.getx() == x + 2 && p.gety() == y + 2) &&
                    checkset.Exists(p => p.getx() == x + 3 && p.gety() == y + 3))
                {
                    return true;
                }

                if (checkset.Exists(p => p.getx() == x + 1 && p.gety() == y - 1) &&
                    checkset.Exists(p => p.getx() == x + 2 && p.gety() == y - 2) &&
                    checkset.Exists(p => p.getx() == x + 3 && p.gety() == y - 3))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsDraw()
        {
            if (BlackEllipsePieces.Count + WhiteEllipsePieces.Count == ROWS * COLS)
            {
                return true;
            }
            return false; //A switch of program , false in normal situation and true for stop
        }

        private Ellipse CreateEllipse(double width, double height, Brush fill)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = width,
                Height = height,
                Fill = fill,
                Effect = new DropShadowEffect { Color = Colors.Black, Direction = 320, ShadowDepth = 5, Opacity = 0.5 }
            };

            return ellipse;
        }

        private void AddEllipseToChessboard(Ellipse ellipse, int row, int col, bool superimpose)
        {
            Grid.SetRow(ellipse, row);
            Grid.SetColumn(ellipse, col);
            if (superimpose)
            {
                double offsetX = chessboard.ColumnDefinitions[0].ActualWidth / 4;
                double offsetY = chessboard.RowDefinitions[0].ActualHeight / 4;
                ellipse.Margin = new Thickness(offsetX, offsetY, 0, 0);
            }
            chessboard.Children.Add(ellipse);
        }

        private void AddEllipseToChessboard(Ellipse ellipse, Position check, bool superimpose)
        {
            Grid.SetRow(ellipse, check.getx());
            Grid.SetColumn(ellipse, check.gety());
            if(superimpose)
            {
                double offsetX = chessboard.ColumnDefinitions[0].ActualWidth / 4; 
                double offsetY = chessboard.RowDefinitions[0].ActualHeight / 4; 
                ellipse.Margin = new Thickness(offsetX, offsetY, 0, 0);
            }
            chessboard.Children.Add(ellipse);
        }

        private int GetAllCount()
        {
            return BlackEllipsePieces.Count + WhiteEllipsePieces.Count;
        }

        public double CalculateWinRate()
        {
            double rate;
            int total1 = 0, total2 = 0;

            foreach (Position p in BlackEllipsePieces)
            {
                total1 += evaluate(p);
            }
            foreach(Position p in WhiteEllipsePieces)
            {
                total2 += evaluate(p);
            }

            rate = (double)total1 / ((double)total1 + (double)total2);
            return rate;
        }
    }


    // Position of a piece
    public class Position
    {
        private int _x, _y;
        public Position(int x = 0, int y = 0)
        {
            _x = x;
            _y = y;
        }

        public int getx()
        {
            return _x;
        }

        public int gety()
        {
            return _y;
        }
    }


    // Node of the MinimaxTree
    public class node
    {
        public double alpha, beta;
        public int depth, value;
        public nodetype type;

        public node(int _depth = 0, int _value = 0, double _alpha = double.NegativeInfinity,
            double _beta = double.PositiveInfinity, nodetype _type = nodetype.max)
        {
            depth = _depth;
            alpha = _alpha;
            beta = _beta;
            value = _value;
            type = _type;
        }
    }


    // An extension
    public class ArrayExtensions
    {
        public int this[Position check, int index]
        {
            get
            {
                if (index == 0)
                    return check.getx();
                else if (index == 1)
                    return check.gety();
                else
                    throw new IndexOutOfRangeException($"Index {index} is out of range for Check type.");
            }
        }
    }

}
