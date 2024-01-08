#version 330 core
out vec4 FragColor;

in VS_OUT {
    vec4 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
    vec4 EyePos;
} fs_in;

#define FOG_TYPE_LINEAR 0
#define FOG_TYPE_EXP 1
#define FOG_TYPE_EXP2 2

struct Fog
{
    vec3 color;
    float start;
    float end;
    float density;
    int type;
};

uniform sampler2D diffuseTexture;
uniform sampler2D shadowMap;

uniform vec3 lightDir;
uniform vec3 viewPos;
uniform vec3 lightColor;
uniform Fog fog;
uniform float exposure;
uniform float gamma;

float CalculateFog(Fog fog, float coord)
{
    float result = 0.0;

	if(fog.type == 0)
	{
		float fogLength = fog.end - fog.start;
		result = (fog.end - coord) / fogLength;
	}
	else if(fog.type == 1)
    {
		result = exp(-fog.density * coord);
	}
	else if(fog.type == 2)
    {
		result = exp(-pow(fog.density * coord, 2.0));
	}
	
	result = 1.0 - clamp(result, 0.0, 1.0);
	return result;
}

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // calculate bias (based on depth map resolution and slope)
    vec3 normal = normalize(fs_in.Normal);
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    
    // check whether current frag pos is in shadow
    // float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;
    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }
    shadow /= 9.0;
    
    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

void main()
{
    vec4 temp = vec4(0);
    vec3 color = texture(diffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    // ambient
    vec3 ambient = 0.3 * lightColor;
    // diffuse
    vec3 lightDir = normalize(lightDir - fs_in.FragPos.xyz);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;
    // specular
    vec3 viewDir = normalize(viewPos - fs_in.FragPos.xyz);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;    
    // calculate shadow
    float shadow = ShadowCalculation(fs_in.FragPosLightSpace);                      
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;    

    float fogCoord = abs(fs_in.EyePos.z / fs_in.EyePos.w);

    temp = mix(vec4(lighting, 1.0), vec4(fog.color, 1.0), CalculateFog(fog, fogCoord));

    vec3 hdrColor = temp.xyz;
  
    vec3 mapped = vec3(1.0) - exp(-hdrColor * exposure);
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}
