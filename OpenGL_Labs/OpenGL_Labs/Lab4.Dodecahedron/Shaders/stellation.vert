#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aFaceColor;

uniform mat4 uModel;
uniform mat4 uNormalMatrix;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vWorldPos;
out vec3 vWorldNormal;
out vec3 vViewPos; // Передаем позицию камеры во фрагментный шейдер
out vec3 vFaceColor;

void main()
{
    vWorldPos = vec3(uModel * vec4(aPos, 1.0));
    
    // Вычисляем матрицу нормалей на лету (или передай её, если есть)
    vWorldNormal = mat3(uNormalMatrix) * aNormal;

    // ИЗВЛЕКАЕМ ПОЗИЦИЮ КАМЕРЫ ИЗ uView
    // Инвертируем матрицу вида. Четвертая колонка инвертированной матрицы - 
    // это положение камеры в мировом пространстве.
    mat4 invView = inverse(uView);
    vViewPos = vec3(invView[3]); 
    vFaceColor = aFaceColor;

    gl_Position = uProjection * uView * vec4(vWorldPos, 1.0);
}