﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MarkovChain
{
    public class MultiDeepMarkovChain : IMarkovChain
    {
        private Dictionary<string, Chain> chains;
        private Chain head;
        private int depth;

        /// <summary>
        /// Creates a new multi-deep Markov Chain with the depth passed in
        /// </summary>
        /// <param name="depth">The depth to store information for words.  Higher values mean more consistency but less flexibility.  Minimum value of three.</param>
        public MultiDeepMarkovChain(int depth)
        {
            if (depth < 3)
                throw new ArgumentException("We currently only support Markov Chains 3 or deeper.  Sorry :(");
            chains = new Dictionary<string, Chain>();
            head = new Chain() { text = "[]" };
            chains.Add("[]", head);
            this.depth = depth;
        }

        /// <summary>
        /// Feed in text that wil be used to create predictive text.
        /// </summary>
        /// <param name="s">The text that this Markov chain will use to generate new sentences</param>
        public void feed(string s)
        {
            s = s.ToLower();
            s = s.Replace("/", "").Replace("\\", "").Replace("[]", "").Replace(",", "");
            s = s.Replace("\r\n\r\n", " ").Replace("\r", "").Replace("\n", " "); //The first line is a hack to fix two \r\n (usually a <p> on a website)
            s = s.Replace(".", " .").Replace("!", " ! ").Replace("?", " ?");

            string[] splitValues = s.Split(' ');
            List<string[]> sentences = getSentences(splitValues);
            string[] valuesToAdd;

            foreach (string[] sentence in sentences)
            {
                for (int start = 0; start < sentence.Length - 1; start++)
                {
                    for (int end = 2; end < depth + 2 && end + start <= sentence.Length; end++)
                    {
                        valuesToAdd = new string[end];
                        for (int j = start; j < start + end ; j++)
                            valuesToAdd[j - start] = sentence[j];
                        addWord(valuesToAdd);
                    }
                }
            }
        }

        private List<string[]> getSentences(string[] words)
        {
            List<string[]> sentences = new List<string[]>();
            List<string> currentSentence = new List<string>();
            currentSentence.Add("[]"); //start of sentence
            for (int i = 0; i < words.Length; i++)
            {
                currentSentence.Add(words[i]);
                if (words[i] == "!" || words[i] == "." || words[i] == "?")
                {
                    sentences.Add(currentSentence.ToArray());
                    currentSentence = new List<string>();
                    currentSentence.Add("[]");
                }
            }
            return sentences;
        }

        private void addWord(string[] words, int count = 1)
        {
            //Note:  This only adds the last word in the array. The other words should already be added by this point
            List<Chain> chainsList = new List<Chain>();
            string lastWord = words[words.Length - 1];
            for (int i = 1; i < words.Length - 1; i++)
                chainsList.Add(this.chains[words[i]]);
            if (!this.chains.ContainsKey(lastWord))
                this.chains.Add(lastWord, new Chain() { text = lastWord });
            chainsList.Add(this.chains[lastWord]);
            Chain firstChainInList = chains[words[0]];
            firstChainInList.addWords(chainsList.ToArray(), count);
        }
        
        /// <summary>
        /// Determines if this Markov Chain is ready to begin generating sentences
        /// </summary>
        /// <returns></returns>
        public bool readyToGenerate()
        {
            return (head.getNextWord() != null);
        }

        /// <summary>
        /// Generate a sentence based on the data passed into this Markov Chain.
        /// </summary>
        /// <returns></returns>
        public string generateSentence()
        {
            StringBuilder sb = new StringBuilder();
            string[] currentChains = new string[depth];
            currentChains[0] = head.getNextWord().text;
            if (currentChains[0] == null) return generateSentence();
            sb.Append(currentChains[0]);
            string[] temp;
            bool doneProcessing = false;
            for (int i = 1; i < depth; i++)
            {
                //Generate the first row
                temp = new string[i];
                for (int j = 0; j < i; j++)
                    temp[j] = currentChains[j];
                currentChains[i] = head.getNextWord(temp).text;
                if (currentChains[i] == "."
                    || currentChains[i] == "?"
                    || currentChains[i] == "!")
                {
                    doneProcessing = true;
                    sb.Append(currentChains[i]);
                    break;
                }
                sb.Append(" ");
                sb.Append(currentChains[i]);
            }

            int breakCounter = 0;
            while (!doneProcessing)
            {
                for (int j = 1; j < depth; j++)
                    currentChains[j - 1] = currentChains[j];
                Chain newHead = chains[currentChains[0]];
                temp = new string[depth - 2];
                for (int j = 1; j < depth - 1; j++)
                    temp[j - 1] = currentChains[j];

                currentChains[depth - 1] = newHead.getNextWord(temp).text;
                if (currentChains[depth - 1] == "." ||
                    currentChains[depth - 1] == "?" ||
                    currentChains[depth - 1] == "!")
                {
                    sb.Append(currentChains[depth - 1]);
                    break;
                }
                sb.Append(" ");
                sb.Append(currentChains[depth - 1]);

                breakCounter++;
                if (breakCounter >= 50) //This is still relatively untested software.  Better safe than sorry :)
                    break;
            }


            sb[0] = char.ToUpper(sb[0]);
            return sb.ToString();
        }

        public List<string> getNextLikelyWord(string previousText)
        {
            //TODO:  Do a code review of this function, it was written pretty hastily
            //TODO:  Include results that use a chain of less length that the depth.  This will allow for more results when the depth is large
            List<string> results = new List<string>();
            previousText = previousText.ToLower();
            previousText = previousText.Replace("/", "").Replace("\\", "").Replace("[]", "").Replace(",", "");
            previousText = previousText.Replace("\r\n\r\n", " ").Replace("\r", "").Replace("\n", " "); //The first line is a hack to fix two \r\n (usually a <p> on a website)
                        
            if (previousText == string.Empty)
            {
                //Assume start of sentence

                List<ChainProbability> nextChains = head.getPossibleNextWords(new string[0]);
                nextChains.Sort((x, y) =>
                {
                    return x.count - y.count;
                });
                foreach (ChainProbability cp in nextChains)
                    results.Add(cp.chain.text);
            }
            else
            {
                string[] initialSplit = previousText.Split(' ');

                string[] previousWords;
                if (initialSplit.Length > depth)
                {
                    previousWords = new string[depth];
                    for (int i = 0; i < depth; i++)
                        previousWords[i] = initialSplit[initialSplit.Length - depth + i];
                }
                else
                {
                    previousWords = new string[initialSplit.Length];
                    for (int i = 0; i < initialSplit.Length; i++)
                        previousWords[i] = initialSplit[i];
                }

                if (!chains.ContainsKey(previousWords[0]))
                    return new List<string>();

                try
                {
                    Chain headerChain = chains[previousWords[0]];
                    string[] sadPreviousWords = new string[previousWords.Length - 1]; //They are sad because I'm allocating extra memory for a slightly different array and there's probably a better way but I'm lazy :(
                    for(int i=1; i<previousWords.Length; i++)
                        sadPreviousWords[i -1] = previousWords[i];
                    List<ChainProbability> nextChains = headerChain.getPossibleNextWords(sadPreviousWords);
                    nextChains.Sort((x, y) =>
                        {
                            return x.count - y.count;
                        });
                    foreach (ChainProbability cp in nextChains)
                        results.Add(cp.chain.text);
                }
                catch (Exception)
                {
                    return new List<string>();
                }
            }
            return results;
        }

        private class Chain
        {
            internal string text;
            internal int fullCount;
            internal Dictionary<string, ChainProbability> nextNodes;

            internal Chain()
            {
                nextNodes = new Dictionary<string, ChainProbability>();
                fullCount = 0;
            }

            internal void addWords(Chain[] c, int count=1)
            {
                if (c.Length == 0)
                    throw new ArgumentException("The array of chains passed in is of zero length.");
                if (c.Length == 1)
                {
                    this.fullCount += count;
                    if (!this.nextNodes.ContainsKey(c[0].text))
                        this.nextNodes.Add(c[0].text, new ChainProbability(c[0], count));
                    else
                        this.nextNodes[c[0].text].count += count;
                    return;
                }

                ChainProbability nextChain = nextNodes[c[0].text];
                for (int i = 1; i < c.Length - 1; i++)
                    nextChain = nextChain.getNextNode(c[i].text);
                nextChain.addWord(c[c.Length - 1],count);
            }

            internal Chain getNextWord()
            {
                int currentCount = RandomHandler.random.Next(fullCount) + 1;
                foreach (string key in nextNodes.Keys)
                {
                    currentCount -= nextNodes[key].count;
                    if (currentCount <= 0)
                        return nextNodes[key].chain;
                }
                return null;
            }

            internal Chain getNextWord(string[] words)
            {
                ChainProbability currentChain = nextNodes[words[0]];
                for (int i = 1; i < words.Length; i++)
                    currentChain = currentChain.getNextNode(words[i]);

                int currentCount = RandomHandler.random.Next(currentChain.count) + 1;
                foreach (string key in currentChain.nextNodes.Keys)
                {
                    currentCount -= currentChain.nextNodes[key].count;
                    if (currentCount <= 0)
                        return currentChain.nextNodes[key].chain;
                }
                return null;
            }

            internal List<ChainProbability> getPossibleNextWords(string[] words)
            {
                List<ChainProbability> results = new List<ChainProbability>();

                if (words.Length == 0)
                {
                    foreach (string key in nextNodes.Keys)
                        results.Add(nextNodes[key]);
                    return results;
                }

                ChainProbability currentChain = nextNodes[words[0]];
                for (int i = 1; i < words.Length; i++)
                    currentChain = currentChain.getNextNode(words[i]);

                foreach (string key in currentChain.nextNodes.Keys)
                    results.Add(currentChain.nextNodes[key]);
                
                return results;
            }
        }

        private class ChainProbability
        {
            internal Chain chain;
            internal int count;
            internal Dictionary<string, ChainProbability> nextNodes;

            internal ChainProbability(Chain c, int co)
            {
                chain = c;
                count = co;
                nextNodes = new Dictionary<string, ChainProbability>();
            }

            internal void addWord(Chain c, int count = 1)
            {
                string word = c.text;
                if (this.nextNodes.ContainsKey(word))
                    this.nextNodes[word].count += count;
                else
                    this.nextNodes.Add(word, new ChainProbability(c, count));
            }

            internal ChainProbability getNextNode(string prev)
            {
                return nextNodes[prev];
            }
        }
    }
}
