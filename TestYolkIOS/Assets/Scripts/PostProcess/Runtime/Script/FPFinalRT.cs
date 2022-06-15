using UnityEngine;

namespace CenturyGame.PostProcess
{
    public class FPFinalRT
    {
        private static FPFinalRT m_Instance;

        public static FPFinalRT instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new FPFinalRT();
                }

                return m_Instance;
            }
        }

        public RenderTexture finalRT;
    }
}