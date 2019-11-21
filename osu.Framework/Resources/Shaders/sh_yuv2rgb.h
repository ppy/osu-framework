uniform sampler2D m_SamplerY;
uniform sampler2D m_SamplerU;
uniform sampler2D m_SamplerV;

const mediump mat3 bt601_coeff = mat3(1.164,  1.164, 1.164,
                                0.0, -0.392, 2.017,
                              1.596, -0.813,   0.0);
const mediump vec3 offsets     = vec3(-0.0625, -0.5, -0.5);

lowp vec3 sampleRgb(vec2 loc) 
{
  float y = texture2D(m_SamplerY, loc).r;
  float u = texture2D(m_SamplerU, loc).r;
  float v = texture2D(m_SamplerV, loc).r;
  return bt601_coeff * (vec3(y, u, v) + offsets);
}