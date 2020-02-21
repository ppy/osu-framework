uniform sampler2D m_SamplerY;
uniform sampler2D m_SamplerU;
uniform sampler2D m_SamplerV;

uniform mediump mat3 yuvCoeff;

// Y - 16, Cb - 128, Cr - 128
const mediump vec3 offsets = vec3(-0.0625, -0.5, -0.5);

lowp vec3 sampleRgb(vec2 loc) 
{
  lowp float y = texture2D(m_SamplerY, loc).r;
  lowp float u = texture2D(m_SamplerU, loc).r;
  lowp float v = texture2D(m_SamplerV, loc).r;
  return yuvCoeff * (vec3(y, u, v) + offsets);
}
