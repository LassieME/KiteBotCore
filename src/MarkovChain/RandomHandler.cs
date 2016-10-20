using System;

namespace MarkovChain
{
    public static class RandomHandler
    {
        //Handles the global random object
        private static System.Random _random;
        public static System.Random random
        {
            get
            {
                if (_random == null)
                    _random = new Random();

                return _random;
            }
        }
    }
}
