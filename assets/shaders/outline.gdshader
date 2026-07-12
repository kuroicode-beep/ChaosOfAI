// assets/shaders/outline.gdshader
// 인버티드 헐(inverted hull) 외곽선: 같은 메시를 노멀 방향으로 살짝 부풀린 뒤 앞면을 컬링해
// 뒷면(=바깥으로 확장된 실루엣)만 단색으로 그린다. 조명과 무관하게 항상 보이는 확실한 외곽선.
// 사용처: DungeonBuilder의 벽(§6 가독성 — 배경과 벽을 색상만이 아니라 윤곽으로도 구분).
shader_type spatial;
render_mode cull_front, unshaded, depth_draw_always;

uniform vec4 outline_color : source_color = vec4(0.0, 0.95, 1.0, 1.0);
uniform float outline_width : hint_range(0.0, 0.5) = 0.06;

void vertex() {
    VERTEX += NORMAL * outline_width;
}

void fragment() {
    ALBEDO = outline_color.rgb;
}
