#define GAMMA 2.2

vec4 toLinear(vec4 colour)
{
	return vec4(pow(colour.rgb, vec3(GAMMA)), colour.a);
}

vec4 toSRGB(vec4 colour)
{
	return vec4(pow(colour.rgb, vec3(1.0 / GAMMA)), colour.a);
}