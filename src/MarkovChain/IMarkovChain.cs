namespace MarkovChain
{
    public interface IMarkovChain
    {
        void feed(string s);

        bool readyToGenerate();

        string generateSentence();
    }
}
