using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reversing_nearness
{
    class Program
    {
        static long[] scoresCalculated = new long[31];
        static ScoreKeeper bestScores;
        static int[][] baselineDists = new int[31][];

        static void Main(string[] args)
        {
            int size = 9;
            if (args.Length >= 1 && !int.TryParse(args[0], out size))
            {
                Log("invalid grid size, unable to parse");
                for(int i = 0; i < args.Length; i++)
                {
                    Log($"   arg {i}: {args[i]}");
                }
                return;
            }
            if (size < 6 || size > 30)
            {
                Log("invalid grid size, must be between 6 and 30");
                return;
            }
            int threads = 1;
            if (args.Length >= 2 && !int.TryParse(args[1], out threads))
            {
                Log("invalid thread count, unable to parse");
                for(int i = 0; i < args.Length; i++)
                {
                    Log($"   arg {i}: {args[i]}");
                }
                return;
            }
            if (threads < 1 || threads > Environment.ProcessorCount - 1)
            {
                Log("invalid thread count, must be between 1 and " + (Environment.ProcessorCount - 1));
                return;
            }
            Log("ReproducibleSeed = " + ThreadSafeRandom.ReproducibleSeed);

            bestScores = new ScoreKeeper("best-scores");

            ComputeBaselines();
            //ComputeLowerBounds();
            
            //for(size = 6; size <= 30; size++)
            //Parallel.For(31-15,31, (size) =>
            Parallel.For(0, threads, _ =>
            {
                Log("initializing", size);
                //GreedyBestNext(size, maxRandomRestarts: 1_000_000_000);
                BuildUp(size, maxRandomRestarts: 1_000_000);
            });

            // for(int size = 6; size < scoresCalculated.Length; size++)
            // {
            //     Log($"total score calculations: {scoresCalculated}", size);
            // }
        }

        static void ComputeLowerBounds()
        {
            for(int size = 2; size < 31; size++)
            {
                var grid = new Grid(size);
                var dists = grid.GetDistances();
                Array.Sort(dists);
                var distsReverse = dists.Reverse().ToArray();
                var lowerBound = Dot(dists, distsReverse);
                Log($"lowerbound {size} => {lowerBound}");
            }
        }

        static void ComputeBaselines()
        {
            for(int i = 6; i < baselineDists.Length; i++)
            {
                baselineDists[i] = new Grid(i).GetDistances();
            }
        }

        static void GreedyBestNext(int size, int maxRandomRestarts = 0)
        {
            int restarts = 0;
            var grid = new Grid(size);
            var bestSuccessors = new List<(int a, int b)>(1000000);

            while(restarts <= maxRandomRestarts)
            {
                grid.Randomize();
                var bestScore = Score(size, grid);
                //Log($"initial score: {bestScore}", size);
                restarts++;

                while(true)
                {
                    // force us to find an improvement
                    bestScore -= 1;

                    // get all best successors
                    bestSuccessors.Clear();
                    var successors = grid.GetSuccessorSwaps();
                    foreach(var swap in successors)
                    {
                        grid.Swap(swap);
                        long score = Score(size, grid);
                        grid.Swap(swap);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestSuccessors.Clear();
                        }
                        if (score == bestScore)
                        {
                            bestSuccessors.Add(swap);
                        }
                    }
                    
                    // pick next best if we found one
                    if (bestSuccessors.Count == 0) break;

                    //Log($"bestScore: {bestScore}", size);
                    var chosenSwap = bestSuccessors.ChooseOne();
                    grid.Swap(chosenSwap);
                }
            }
        }


        static void BuildUp(int size, int maxRandomRestarts = 0)
        {
            int restarts = 0;
            while(restarts <= maxRandomRestarts)
            {
                restarts += 1;

                int[] cells = new int[size*size];
                for(int i = 1; i < cells.Length; i++) cells[i] = -1;

                var remainingValues = Enumerable.Range(1, cells.Length-1)
                    .ToHashSet();

                while(remainingValues.Count > 0)
                {
                    var bestScore = long.MaxValue;
                    var bestValueLocations = new List<(int cell, int value)>();
                    for(int i = 0; i < cells.Length; i++)
                    {
                        if (cells[i] >= 0) continue;
                        foreach(var value in remainingValues)
                        {
                            cells[i] = value;
                            var dists = BuildUpDists(cells, size);
                            // var score = BuildUpScore(dists, baselineDists[size]);
                            // if (score > bestScore)
                            // {
                            //     bestScore = score;
                            //     bestValueLocations.Clear();
                            // }
                            // if (score == bestScore)
                            // {
                            //     bestValueLocations.Add((i, value));
                            // }
                            var score = BuildUpScoreLow(size, dists);
                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestValueLocations.Clear();
                            }
                            if (score == bestScore)
                            {
                                bestValueLocations.Add((i, value));
                            }
                        }
                        cells[i] = -1;
                    }

                    var next = bestValueLocations.ChooseOne();
                    cells[next.cell] = next.value;
                    remainingValues.Remove(next.value);
                    //Log($"{next.cell} => {next.value} -- remaining: {remainingValues.Count}", size);
                }

                var grid = new Grid(size, cells);
                //var greedyFinishScore = BuildUpGreedyBestNext(size, grid);
                var greedyFinishScore = Score(size, grid);
                Log(greedyFinishScore, size);
            }
        }

        static long BuildUpGreedyBestNext(int size, Grid grid)
        {
            var bestSuccessors = new List<(int a, int b)>(1000000);

            var bestScore = Score(size, grid);
            while(true)
            {
                // force us to find an improvement
                bestScore -= 1;

                // get all best successors
                bestSuccessors.Clear();
                var successors = grid.GetSuccessorSwaps();
                foreach(var swap in successors)
                {
                    grid.Swap(swap);
                    long score = Score(size, grid);
                    grid.Swap(swap);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestSuccessors.Clear();
                    }
                    if (score == bestScore)
                    {
                        bestSuccessors.Add(swap);
                    }
                }
                
                // pick next best if we found one
                if (bestSuccessors.Count == 0) break;

                //Log($"bestScore: {bestScore}", size);
                var chosenSwap = bestSuccessors.ChooseOne();
                grid.Swap(chosenSwap);
            }

            return bestScore;
        }

        static int[] BuildUpDists(int[] Cells, int size)
        {
            var lookup = new Dictionary<int, int>();
            for(int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] < 0) continue;
                lookup.Add(Cells[i], i);
            }
            int[] result = baselineDists[size].ToArray();
            int totalCells = Cells.Length;
            int halfDist = size / 2;
            int currentResultIndex = 0;
            for(int i = 0; i < totalCells; i++)
            {
                if (!lookup.ContainsKey(i)) continue;
                int i_loc = lookup[i];
                int i_row = i_loc / size;
                int i_col = i_loc % size;
                for(int j = i+1; j < totalCells; j++)
                {
                    if (!lookup.ContainsKey(j)) continue;
                    int j_loc = lookup[j];
                    int j_row = j_loc / size;
                    int j_col = j_loc % size;
                    
                    int rowDiff = Math.Abs(j_row - i_row);
                    rowDiff = rowDiff > halfDist ? (size - rowDiff) : rowDiff;
                    int colDiff = Math.Abs(j_col - i_col);
                    colDiff = colDiff > halfDist ? (size - colDiff) : colDiff;
                    
                    result[currentResultIndex++] = rowDiff*rowDiff + colDiff*colDiff;
                }
            }
            return result;
        }

        static long BuildUpScore(int[] dists_a, int[] dists_b)
        {
            long total = 0L;
            for(int i = 0; i < dists_a.Length; i++)
            {
                long diff = dists_a[i] - dists_b[i];
                total += diff * diff;
            }
            return total;
        }

        static long BuildUpScoreLow(int size, int[] dists)
        {
            var gridDists = dists;
            var baseline = baselineDists[size];
            var score = Dot(baseline, gridDists) - lowerbounds[size];
            return score;
        }

        static long Score(int size, Grid grid)
        {
            //var calcs = Interlocked.Increment(ref scoresCalculated[size]);
            // if (calcs % 1_000_000 == 0)
            // {
            //     Log($"scoresCalculated: {calcs}", size);
            // }
            var gridDists = grid.GetDistances();
            var baseline = baselineDists[size];
            var score = Dot(baseline, gridDists) - lowerbounds[size];
            if (bestScores.IsBest(size, score))
            {
                bestScores.UpdateBest(size, score, grid.ToString());
                Log($"new best score! = {score}", size);
            }
            return score;
        }

        static void Log(object value, int? size = null)
        {
            string sizeNote = size.HasValue ? $"(size: {size}) - " : "";
            Console.WriteLine($"[{DateTime.UtcNow}] {sizeNote}{value}");
        }

        static long Dot(int[] a, int[] b)
        {
            long result = 0;
            for(int i = 0; i < a.Length; i++)
            {
                result += a[i] * b[i];
            }
            return result;
        }

        static long[] lowerbounds = new[]
        {
            0L,
            0L,
            10L,
            72L,
            816L,
            3800L,
            16902L,
            52528L,
            155840L,
            381672L,
            902550L,
            1883244L,
            3813912L,
            7103408L,
            12958148L,
            22225500L,
            37474816L,
            60291180L,
            95730984L,
            146469252L,
            221736200L,
            325763172L,
            474261920L,
            673706892L,
            949783680L,
            1311600000L,
            1799572164L,
            2425939956L,
            3252444776L,
            4294801980L,
            5643997650L,
        };
    }

    class Grid
    {
        public int[] Cells { get; private set; }
        private int pairCount;
        private int size;

        public Grid(int size)
        {
            this.size = size;
            int n = size*size;
            Cells = Enumerable.Range(0, n).ToArray();
            pairCount = (n)*(n-1)/2;
        }

        public Grid(int size, int[] cells) 
            : this(size)
        {
            if (Cells.Length != cells.Length) throw new Exception("Wrong number of cells!");
            Cells = cells;
        }

        public int[] GetDistances()
        {
            var lookup = new Dictionary<int, int>();
            for(int i = 0; i < Cells.Length; i++)
            {
                lookup.Add(Cells[i], i);
            }
            int[] result = new int[pairCount];
            int totalCells = Cells.Length;
            int halfDist = size / 2;
            int currentResultIndex = 0;
            for(int i = 0; i < totalCells; i++)
            {
                int i_loc = lookup[i];
                int i_row = i_loc / size;
                int i_col = i_loc % size;
                for(int j = i+1; j < totalCells; j++)
                {
                    int j_loc = lookup[j];
                    int j_row = j_loc / size;
                    int j_col = j_loc % size;
                    
                    int rowDiff = Math.Abs(j_row - i_row);
                    rowDiff = rowDiff > halfDist ? (size - rowDiff) : rowDiff;
                    int colDiff = Math.Abs(j_col - i_col);
                    colDiff = colDiff > halfDist ? (size - colDiff) : colDiff;
                    
                    result[currentResultIndex++] = rowDiff*rowDiff + colDiff*colDiff;
                }
            }
            return result;
        }

        public IEnumerable<(int a, int b)> GetSuccessorSwaps()
        {
            var successors = new List<Grid>();
            for(int i = 1; i < Cells.Length; i++)
            for(int j = i+1; j < Cells.Length; j++)
            {
                yield return (i, j);
            }
            yield break;
        }

        public void Swap((int a, int b) swap)
        {
            var a = Cells[swap.a];
            Cells[swap.a] = Cells[swap.b];
            Cells[swap.b] = a;
        }

        public void Randomize()
        {
            for(int i = 1; i < Cells.Length; i++)
            {
                var j = ThreadSafeRandom.Random.Next(i, Cells.Length);
                Swap((i, j));
            }
        }

        public override string ToString()
        {
            // generate string representation
            var sb = new StringBuilder();
            for(int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(",\n");
                sb.Append("(");
                for(int j = 0; j < size; j++)
                {
                    int value = Cells[i*size+j];
                    int original_row = value / size;
                    int original_col = value % size;

                    if (j > 0) sb.Append(", ");
                    sb.Append(cellname[original_row]);
                    sb.Append(cellname[original_col]);
                }
                sb.Append(")");
            }
            return sb.ToString();
        }

        static char[] cellname = new[]
        {
            'A', 'B', 'C', 'D', 'E',
            'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O',
            'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y',
            'Z',
            '1', '2', '3', '4', '5',
        };
    }
}
