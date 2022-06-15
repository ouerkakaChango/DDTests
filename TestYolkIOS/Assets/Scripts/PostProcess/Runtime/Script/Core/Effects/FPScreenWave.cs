using UnityEngine;

/*
	点击水波效果
*/

namespace CenturyGame.PostProcess
{
	public class FPScreenWave : IPostProcess
	{
		private float m_distanceFactor = 0;
		private float m_waveSpeed = 0;
		private float m_waveWidth = 0;
		private float m_totalFactor = 0;
		private float m_timeFactor = 0;
		private float m_waterWaveRang = 0;

		private float m_waveStartTime;
		private Shader m_curShader;
		private Material m_curMat;

		//距离系数
		public float distanceFactor = 60f;

		//时间系数
		public float timeFactor = -20f;

		// sin函数结果系数
		public float totalFactor = 1.0f;

		// 波纹宽度
		public float waveWidth = 0.4f;

		// 波纹扩散的速度
		public float waveSpeed = 0.8f;

		// 扩散范围
		public float waterWaveRang = 0.1f;

		public override void Init()
		{
			Title = "FPScreenWave";
			Propertys = new string[] { "distanceFactor", "timeFactor", "totalFactor", "waveWidth", "waveSpeed", "waterWaveRang" };
			checkSupport();
		}

		public override void DoEnable(CenturyGame.PostProcess.PostProcessHandle cam)
		{
			checkSupport();

			m_waveStartTime = Time.time;
		}

		public override void DoDisable()
		{
			if (m_curMat != null)
			{
				GameObject.DestroyImmediate(m_curMat);
				m_curMat = null;
			}
		}

		private void checkSupport()
		{
			if (m_curShader == null)
			{
				m_curShader = CenturyGame.PostProcess.PostProcessHandle.LoadShader("Shaders/Post/FPScreenWave");
			}
			if (!SystemInfo.supportsImageEffects || m_curShader == null || !m_curShader.isSupported)
			{
				Enable = false;
				return;
			}
			if (Enable && m_curMat == null)
			{
				m_curMat = new Material(m_curShader);
				m_curMat.hideFlags = HideFlags.HideAndDontSave;
			}

			// 暂时关闭
			//Enable = false;

			m_distanceFactor = 0f;
			m_timeFactor = 0f;
			m_totalFactor = 0f;
			m_waveWidth = 0f;
			m_waveSpeed = 0f;
			m_waterWaveRang = 0f;
		}

		public override void OnRenderHandle(ref RenderTexture source, ref RenderTexture destination, ref RenderTexture depth, ref int count)
		{
			if (m_curMat == null)
			{
				return;
			}

			if (m_distanceFactor != distanceFactor)
			{
				m_distanceFactor = distanceFactor;
				m_curMat.SetFloat("_distanceFactor", m_distanceFactor);
				m_waveStartTime = Time.time;
			}
			if (m_timeFactor != timeFactor)
			{
				m_timeFactor = timeFactor;
				m_curMat.SetFloat("_timeFactor", m_timeFactor);
				m_waveStartTime = Time.time;
			}

			if (m_totalFactor != totalFactor)
			{
				m_totalFactor = totalFactor;
				m_curMat.SetFloat("_totalFactor", m_totalFactor);
				m_waveStartTime = Time.time;
			}
			if (m_waveWidth != waveWidth)
			{
				m_waveWidth = waveWidth;
				m_curMat.SetFloat("_waveWidth", m_waveWidth);
				m_waveStartTime = Time.time;
			}

			if (m_waveSpeed != waveSpeed)
			{
				m_waveSpeed = waveSpeed;
				m_waveStartTime = Time.time;
			}

			if (m_waterWaveRang != waterWaveRang)
			{
				m_waterWaveRang = waterWaveRang;
				m_curMat.SetFloat("_waterWaveRang", m_waterWaveRang);
				m_waveStartTime = Time.time;
			}

			float curWaveDistance = (Time.time - m_waveStartTime) * waveSpeed;
			if (curWaveDistance > 1)
			{
				return;
			}
			m_curMat.SetFloat("_curWaveDis", curWaveDistance);

			Graphics.Blit(source, destination, m_curMat, 0);

			base.OnRenderHandle(ref source, ref destination, ref depth, ref count);
		}

		public override void Update()
		{
			if (Application.isMobilePlatform)
			{
				for (int i = 0; i < Input.touchCount; ++i)
				{
					Touch touch = Input.GetTouch(i);
					if (touch.phase == TouchPhase.Began)
					{
						var position = (touch.position);
						m_waveStartTime = Time.time;
						m_curMat.SetFloat("_centerPosX", position.x);
						m_curMat.SetFloat("_centerPosY", position.y);
					}
				}
			}
			else
			{
				if (Input.GetMouseButtonDown(0))
				{
					var x = Input.mousePosition.x / Screen.width;
					var y = Input.mousePosition.y / Screen.height;

					m_waveStartTime = Time.time;

					m_curMat.SetFloat("_centerPosX", x);
					m_curMat.SetFloat("_centerPosY", y);
				}
			}
		}
	}
}