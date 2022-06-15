using System;

namespace CenturyGame.PostProcess
{
    [Serializable]
    public class PostProcessItem
    {
        public string Name = null;
        public bool Enable = false;
        public PostProcessItemKV[] PList = null;
    }
}