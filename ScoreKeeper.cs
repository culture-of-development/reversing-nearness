using System;
using System.IO;

namespace reversing_nearness
{
    class ScoreKeeper
    {
        private string path;
        private long[] bestScores;
        private object updateLock = new object();

        public ScoreKeeper(string path)
        {
            this.path = path;
            this.bestScores = new long[31];
            for(int i = 0; i < bestScores.Length; i++)
            {
                bestScores[i] = long.MaxValue;
            }

            LoadFiles();
        }

        private void LoadFiles()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var files = Directory.GetFiles(path, "*.txt");
            foreach(var file in files)
            {
                string content = File.ReadAllText(file);
                int firstLineBreak = content.IndexOf('\n');
                if (firstLineBreak < 0) throw new Exception("output file does not contain the correct content: " + file);
                long score = long.Parse(content.Substring(0, firstLineBreak));
                int size = int.Parse(new FileInfo(file).Name.Replace("best-", "").Replace(".txt", ""));
                bestScores[size] = score;
            }
        }

        public bool IsBest(int size, long score)
        {
            return score < bestScores[size];
        }

        public void UpdateBest(int size, long score, string value)
        {
            if (!IsBest(size, score)) return;
            lock(updateLock)
            {
                if (!IsBest(size, score)) return;
                bestScores[size] = score;
                string filename = Path.Combine(path, $"best-{size}.txt");
                string content = score.ToString() + "\n" + value;
                File.WriteAllText(filename, content);
            }
        }
    }
}