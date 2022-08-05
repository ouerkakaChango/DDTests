UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);

float4 _ShadowMapTexture_TexelSize;

float SampleShadowPCF(float3 uvd)
{
	return UNITY_SAMPLE_SHADOW(_ShadowMapTexture,uvd);
	//采样ShadowMap函数，Z是深度
}

float SampleShadowPCF(float2 uv, float d)
{
	return SampleShadowPCF(float3(uv,d));
}

float SampleShadowPCF3x3_4Tap_Fast(float3 uvd)
//3x3原本是9个像素采样点，优化后为4个采样点
{
	float offsetX = _ShadowMapTexture_TexelSize.x * 0.5;
	float offsetY = _ShadowMapTexture_TexelSize.y * 0.5;
	float4 result;
	result.x = SampleShadowPCF(float3(uvd.x - offsetX, uvd.y - offsetY, uvd.z));
	result.y = SampleShadowPCF(float3(uvd.x + offsetX, uvd.y - offsetY, uvd.z));
	result.z = SampleShadowPCF(float3(uvd.x - offsetX, uvd.y + offsetY, uvd.z));
	result.w = SampleShadowPCF(float3(uvd.x + offsetX, uvd.y + offsetY, uvd.z));
	return dot(result,0.25);
}

static void GetTent5Weights(float kernelOffset,out float4 weightsA,out float2 weightsB)
//根据竖截面的面积计算像素的权重
{
	float a = 0.5 - kernelOffset;
	float b = 0.5 + kernelOffset;
	float c = max(0,-kernelOffset);
	float d = max(0,kernelOffset);
	float w1 = a * a * 0.5;
	float w2 = (2 * a + 1) * 0.5;
	float w3 = (2 + a) * (2 + a) * 0.5 - w1 - w2 - c * c;

	float w6 = b * b * 0.5;
	float w5 = (2 * b + 1) * 0.5;
	float w4 = (2 + b) * (2 + b) * 0.5 - w5 - w6 - d * d;

	weightsA = float4(w1,w2,w3,w4);
	weightsB = float2(w5,w6);
}

///一个group由2x2个像素组成，根据像素的横向权重weightsX和纵向权重weightsY，利用双线性插值公式，计算出采样坐标。
static float2 GetGroupTapUV(float2 groupCenterCoord,float2 weightsX,float2 weightsY)
{
	float offsetX = weightsX.y / (weightsX.x + weightsX.y);
	float offsetY = weightsY.y / (weightsY.x + weightsY.y);
	float2 coord = groupCenterCoord - 0.5 + float2(offsetX,offsetY);
	return coord * _ShadowMapTexture_TexelSize.xy;
}

///5x5的TentFilter，对应36个像素，对应9个group，9个采样点
static void GetTent5GroupWeights(
float4 weightsXA,float2 weightsXB,
float4 weightsYA,float2 weightsYB,
out float3 groupWeightsA,out float3 groupWeightsB,out float3 groupWeightsC)
{
	
	groupWeightsA.x = dot(weightsXA.xyxy,weightsYA.xxyy);
	groupWeightsA.y = dot(weightsXA.zwzw,weightsYA.xxyy);
	groupWeightsA.z = dot(weightsXB.xyxy,weightsYA.xxyy);

	groupWeightsB.x = dot(weightsXA.xyxy,weightsYA.zzww);
	groupWeightsB.y = dot(weightsXA.zwzw,weightsYA.zzww);
	groupWeightsB.z = dot(weightsXB.xyxy,weightsYA.zzww);

	groupWeightsC.x = dot(weightsXA.xyxy,weightsYB.xxyy);
	groupWeightsC.y = dot(weightsXA.zwzw,weightsYB.xxyy);
	groupWeightsC.z = dot(weightsXB.xyxy,weightsYB.xxyy);
	float w = dot(groupWeightsA,1) + dot(groupWeightsB,1) + dot(groupWeightsC,1);
	float iw = rcp(w);
	groupWeightsA *= iw;
	groupWeightsB *= iw;
	groupWeightsC *= iw;
}

float SampleShadowPCF5x5_9Tap(float3 uvd)
{
	float2 texelCoord = _ShadowMapTexture_TexelSize.zw * uvd.xy;
	float2 texelOriginal = round(texelCoord);
	float2 kernelOffset = texelCoord - texelOriginal;

	float4 weightsXA,weightsYA;
	float2 weightsXB,weightsYB;

	GetTent5Weights(kernelOffset.x,weightsXA,weightsXB);
	GetTent5Weights(kernelOffset.y,weightsYA,weightsYB);

	float2 uv0 = GetGroupTapUV(texelOriginal + float2(-2,-2),weightsXA.xy,weightsYA.xy);
	float2 uv1 = GetGroupTapUV(texelOriginal + float2(0,-2),weightsXA.zw,weightsYA.xy);
	float2 uv2 = GetGroupTapUV(texelOriginal + float2(2,-2),weightsXB.xy,weightsYA.xy);

	float2 uv3 = GetGroupTapUV(texelOriginal + float2(-2,0),weightsXA.xy,weightsYA.zw);
	float2 uv4 = GetGroupTapUV(texelOriginal + float2(0,0),weightsXA.zw,weightsYA.zw);
	float2 uv5 = GetGroupTapUV(texelOriginal + float2(2,0),weightsXB.xy,weightsYA.zw);

	float2 uv6 = GetGroupTapUV(texelOriginal + float2(-2,2),weightsXA.xy,weightsYB.xy);
	float2 uv7 = GetGroupTapUV(texelOriginal + float2(0,2),weightsXA.zw,weightsYB.xy);
	float2 uv8 = GetGroupTapUV(texelOriginal + float2(2,2),weightsXB.xy,weightsYB.xy);

	float3 groupWeightsA,groupWeightsB,groupWeightsC;
	///5x5的TentFilter，对应9个Group
	GetTent5GroupWeights(weightsXA,weightsXB,weightsYA,weightsYB,groupWeightsA,groupWeightsB,groupWeightsC);

	float3 tapA,tapB,tapC;
	float d = uvd.z;

	tapA.x = SampleShadowPCF(uv0,d);
	tapA.y = SampleShadowPCF(uv1,d);
	tapA.z = SampleShadowPCF(uv2,d);

	tapB.x = SampleShadowPCF(uv3,d);
	tapB.y = SampleShadowPCF(uv4,d);
	tapB.z = SampleShadowPCF(uv5,d);

	tapC.x = SampleShadowPCF(uv6,d);
	tapC.y = SampleShadowPCF(uv7,d);
	tapC.z = SampleShadowPCF(uv8,d);
	
	return dot(tapA,groupWeightsA) + dot(tapB,groupWeightsB) + dot(tapC,groupWeightsC); 
}