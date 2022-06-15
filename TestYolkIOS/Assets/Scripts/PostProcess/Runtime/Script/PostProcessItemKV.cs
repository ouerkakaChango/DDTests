using System;
using System.Reflection;
using UnityEngine;

namespace CenturyGame.PostProcess
{
	[Serializable]
	public class PostProcessItemKV
	{
		public string Key = null;

		public bool IsUO = false;

		public string Value = null;
		public UnityEngine.Object Obj = null;
		public AnimationCurve ValueCurve;

		public void GetValue(object o)
		{
			Type type = o.GetType();
			FieldInfo fi = type.GetField(this.Key);
			object v = fi.GetValue(o);
			bool flag = fi.FieldType.IsSubclassOf(typeof(UnityEngine.Object));
			if (flag)
			{
				this.IsUO = true;
				bool flag2 = v != null;
				if (flag2)
				{
					this.Obj = (v as UnityEngine.Object);
				}
			}
			else if (fi.FieldType == typeof(AnimationCurve))
			{
				ValueCurve = (AnimationCurve)v;
			}
			else
			{
				bool flag3 = fi.FieldType == typeof(Color);
				if (flag3)
				{
					bool flag4 = v != null;
					if (flag4)
					{
						Color c = (Color)v;
						this.Value = ColorUtility.ToHtmlStringRGB(c);
					}
				}
				else
				{
					bool flag5 = v != null;
					if (flag5)
					{
						this.Value = v.ToString();
					}
				}
			}
		}

		public void SetValue(object o)
		{
			Type type = o.GetType();
			FieldInfo fi = type.GetField(this.Key);
			try
			{
				bool isUO = this.IsUO;
				if (isUO)
				{
					bool flag = this.Obj != null;
					if (flag)
					{
						fi.SetValue(o, this.Obj);
					}
				}
				else if (fi.FieldType == typeof(AnimationCurve))
				{
					fi.SetValue(o, ValueCurve);
				}
				else
				{
					bool flag2 = this.Value != null;
					if (flag2)
					{
						bool flag3 = fi.FieldType == typeof(string);
						if (flag3)
						{
							fi.SetValue(o, this.Value);
						}
						else
						{
							bool flag4 = fi.FieldType == typeof(bool);
							if (flag4)
							{
								fi.SetValue(o, bool.Parse(this.Value));
							}
							else
							{
								bool flag5 = fi.FieldType == typeof(Color);
								if (flag5)
								{
									Color newColor = Color.black;
									ColorUtility.TryParseHtmlString(this.Value, out newColor);
									fi.SetValue(o, newColor);
								}
								else
								{
									bool flag6 = fi.FieldType == typeof(int);
									if (flag6)
									{
										fi.SetValue(o, int.Parse(this.Value));
									}
									else
									{
										bool isEnum = fi.FieldType.IsEnum;
										if (isEnum)
										{
											fi.SetValue(o, Enum.Parse(fi.FieldType, this.Value));
										}
										else
										{
											if (fi.FieldType == typeof(float))
											{
												fi.SetValue(o, float.Parse(this.Value));
											}
											else if (fi.FieldType == typeof(Vector4))
											{
												fi.SetValue(o, ParseVector4(this.Value));
											}
											else
												throw new Exception("Type not support!");
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Log(string.Concat(new string[]
				{
				o.ToString(),
				",",
				this.Key,
				",",
				ex.ToString()
				}));
			}
		}

		Vector4 ParseVector4(string str)
		{
			string[] temp = str.Substring(1, str.Length - 2).Split(',');
			float x = float.Parse(temp[0]);
			float y = float.Parse(temp[1]);
			float z = float.Parse(temp[2]);
			float w = float.Parse(temp[3]);
			Vector4 rValue = new Vector4(x, y, z, w);
			return rValue;
		}
	}
}