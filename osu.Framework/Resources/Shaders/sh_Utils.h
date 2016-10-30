vec4 toLinear(vec4 colour)
{
	return vec4(pow(colour.rgb, vec3(2.2)), colour.a);
}