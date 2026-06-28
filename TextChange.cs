using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Global;

/// <summary>
/// TextChange
/// 
/// 这个脚本负责对 TextMeshPro 文本进行“逐字符运动控制”。
/// 
/// 项目用途：
/// 用动态扰动文字的方式，模拟一种视觉层面的阅读困难体验。
/// 
/// 核心思路：
/// 1. 初始化时读取 TMP_Text 中每个可见字符的顶点信息。
/// 2. 记录每个字符的原始中心点、材质索引、顶点索引。
/// 3. 根据 gazePoint 判断字符是否进入注视影响范围。
/// 4. 字符进入范围后，从 Stop 进入 Move。
/// 5. Move 状态下字符逐渐远离原位，并带有旋转。
/// 6. 达到 orbit_radius 后进入 Spin，持续绕原位附近旋转。
/// 7. 当字符原始位置离开注视范围后进入 Back，逐渐回归原排版位置。
/// 8. 每帧先恢复原始顶点，再根据字符状态重新计算扰动后的顶点。
/// 
/// 注意：
/// 本脚本直接修改 TMP 的 mesh 顶点，因此适合做文字视觉特效。
/// 但它不是医学意义上的阅读障碍模拟，只是视觉干扰原型。
/// </summary>
public class TextChange : MonoBehaviour
{
    /// <summary>
    /// 单个字符的运动状态。
    /// 
    /// Stop:
    /// 字符保持原始排版位置，不参与扰动。
    /// 
    /// Move:
    /// 字符刚进入注视范围，开始从原始位置向外扩散。
    /// 
    /// Spin:
    /// 字符已经扩散到最大轨道半径，在原位附近持续旋转。
    /// 
    /// Back:
    /// 字符离开注视范围后，逐渐回到原始排版位置。
    /// </summary>
    enum Text_Status
    {
        Stop,
        Move,
        Spin,
        Back
    }

    /// <summary>
    /// 单个字符的运行时数据。
    /// 
    /// 这里不要理解成“文字内容”，而是 TMP 中某一个可见字符的运动信息。
    /// 每个字符都有自己的顶点位置、原始中心点、当前角度、当前偏移距离和状态。
    /// </summary>
    struct Text
    {
        /// <summary>
        /// 字符所属的材质索引。
        /// 
        /// TextMeshPro 可能因为字体、材质、Sprite 等原因产生多个 submesh。
        /// 修改顶点时必须知道字符属于哪个 meshInfo。
        /// </summary>
        public int materialIndex;

        /// <summary>
        /// 字符左下角顶点在 vertices 数组中的起始索引。
        /// 
        /// TMP 中一个可见字符一般对应 4 个顶点：
        /// vertexIndex + 0
        /// vertexIndex + 1
        /// vertexIndex + 2
        /// vertexIndex + 3
        /// </summary>
        public int vertexIndex;

        /// <summary>
        /// 当前字符运动角度，单位是弧度。
        /// 
        /// Move / Spin / Back 都会根据这个角度计算偏移方向：
        /// x = cos(angle) * distance
        /// y = sin(angle) * distance
        /// </summary>
        public float angle;

        /// <summary>
        /// 当前字符距离原始中心点的扰动距离。
        /// 
        /// Move 状态下逐渐增加；
        /// Spin 状态下保持在 orbit_radius；
        /// Back 状态下逐渐减少到 0。
        /// </summary>
        public float distance;

        /// <summary>
        /// 注视点移动造成的额外偏移残留。
        /// 
        /// 这个变量主要用来制造“拖尾”或“文字跟不上视线移动”的感觉。
        /// 它会在 ChangeOffset / Back_ChangeOffset 中被逐步消耗。
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// 当前中心点。
        /// 
        /// 当前版本中主要保留为状态记录使用。
        /// 由于每帧会 ResetVertices，再通过 ApplyOffset 重算顶点，
        /// 真正决定字符显示位置的是 origCenter + offset。
        /// </summary>
        public Vector2 center;

        /// <summary>
        /// 字符原始中心点。
        /// 
        /// 这是字符没有被扰动时的排版位置。
        /// 所有运动最终都以它为基准：
        /// - 判断是否进入注视区域
        /// - 计算字符回归
        /// - 计算顶点旋转中心
        /// </summary>
        public Vector2 origCenter;

        /// <summary>
        /// Back 状态下角度回归速度，单位是弧度/秒。
        /// 
        /// 当前默认值为 180 度/秒。
        /// </summary>
        public float back_angleSpeed;

        /// <summary>
        /// Back 状态下半径回归速度。
        /// 
        /// 当前默认值为 150 Unity 坐标单位/秒。
        /// </summary>
        public float back_radiusSpeed;

        /// <summary>
        /// 当前字符状态。
        /// </summary>
        public Text_Status status;

        /// <summary>
        /// 字符数据构造函数。
        /// 初始化一个可见字符的基础运动信息。
        /// </summary>
        /// <param name="materialIndex">字符所在的 TMP 材质索引</param>
        /// <param name="vertexIndex">字符顶点起始索引</param>
        /// <param name="distance">字符到注视点或原位的初始距离</param>
        /// <param name="center">字符原始中心点</param>
        public Text(int materialIndex, int vertexIndex, float distance, Vector2 center)
        {
            this.materialIndex = materialIndex;
            this.vertexIndex = vertexIndex;
            this.angle = 0f;
            this.distance = distance;
            this.offset = new(0, 0);
            this.center = center;
            this.origCenter = center;

            // 回归时的默认角速度与半径速度。
            // 这里直接写死，是为了让半成品原型保持简单。
            // 后续也可以挪到 GlobalConfig 中统一配置。
            this.back_angleSpeed = 180f * Mathf.Deg2Rad;
            this.back_radiusSpeed = 150f;

            this.status = Text_Status.Stop;
        }
    }

    /// <summary>
    /// 需要被扰动的 TextMeshPro 文本组件。
    /// 
    /// 在 Inspector 中拖入对应 TMP_Text。
    /// </summary>
    public TMP_Text text;

    /// <summary>
    /// TMP 当前文本信息。
    /// 
    /// 里面包含 characterInfo、meshInfo、characterCount 等数据。
    /// 每次 ForceMeshUpdate 后，这里会保存最新文本布局信息。
    /// </summary>
    private TMP_TextInfo textInfo = new();

    /// <summary>
    /// 原始顶点数据备份。
    /// 
    /// 因为本脚本每帧都会修改 TMP 顶点，
    /// 所以必须保存一份“没有扰动时”的顶点数据。
    /// 每帧先 ResetVertices 回到原始状态，
    /// 再重新应用当前帧的扰动。
    /// 
    /// 这样可以避免顶点偏移一帧一帧累积，导致文字越飞越远。
    /// </summary>
    private TMP_MeshInfo[] origMeshInfo;

    /// <summary>
    /// 所有字符的运行时数据数组。
    /// 数组下标与 textInfo.characterInfo 的字符下标对应。
    /// </summary>
    private Text[] texts;

    /// <summary>
    /// 调试计时器。
    /// 当前只用于 Debug_judge，每隔一秒输出状态数量。
    /// </summary>
    private float debugTimer = 0f;

    /// <summary>
    /// 调试计数器。
    /// 当前只用于 Debug_count。
    /// </summary>
    private static int debugCount = 0;

    /// <summary>
    /// Stop 状态字符列表。
    /// 
    /// 存储的是 texts 数组中的下标，而不是字符对象本身。
    /// </summary>
    public List<int> stopList = new List<int>();

    /// <summary>
    /// Move 状态字符列表。
    /// 字符正在从原始位置向外扩散。
    /// </summary>
    public List<int> movingList = new List<int>();

    /// <summary>
    /// Spin 状态字符列表。
    /// 字符已经到达最大扰动半径，正在持续旋转。
    /// </summary>
    public List<int> spinList = new List<int>();

    /// <summary>
    /// Back 状态字符列表。
    /// 字符正在回归原始排版位置。
    /// </summary>
    public List<int> back_MovingList = new List<int>();

    /// <summary>
    /// 本帧发生过顶点变化的材质索引集合。
    /// 
    /// 一个 TMP_Text 可能有多个 mesh / material。
    /// 修改完顶点后，只更新发生变化的 mesh，避免无意义更新。
    /// </summary>
    public HashSet<int> ChangedMesh = new HashSet<int>();

    /// <summary>
    /// Move 状态下的旋转角速度。
    /// 
    /// GlobalConfig 中以“度/秒”配置，
    /// 这里转换为 Mathf.Sin / Mathf.Cos 使用的“弧度/秒”。
    /// </summary>
    private readonly float move_rotateSpeed = GlobalConfig.move_rotateSpeed * Mathf.Deg2Rad;

    /// <summary>
    /// Spin 状态下的旋转角速度。
    /// 
    /// 同样从“度/秒”转换为“弧度/秒”。
    /// </summary>
    private readonly float spin_rotateSpeed = GlobalConfig.spin_rotateSpeed * Mathf.Deg2Rad;

    /// <summary>
    /// 初始化 TMP 字符信息。
    /// 
    /// 这个函数会：
    /// 1. 强制 TMP 刷新 Mesh。
    /// 2. 备份原始顶点数据。
    /// 3. 为每个可见字符创建 Text 数据。
    /// 4. 根据字符与 gazePoint 的距离，决定初始状态是 Stop 还是 Move。
    /// 5. 清空所有状态列表，重新分类字符。
    /// 
    /// 当文本内容、字体、排版属性发生变化时，需要重新调用。
    /// </summary>
    /// <returns>初始化后的字符数据数组</returns>
    private Text[] InitTextInfo()
    {
        // 强制 TMP 更新文本布局和 Mesh 顶点。
        // 如果不调用，textInfo 中的数据可能不是最新的。
        text.ForceMeshUpdate();

        textInfo = text.textInfo;

        // 复制一份原始顶点数据，后续每帧都以这份数据为基础重新计算。
        origMeshInfo = textInfo.CopyMeshInfoVertexData();

        // 按字符数量创建数据数组。
        texts = new Text[(int)textInfo.characterCount];

        // 标记 TMP 属性已经被当前脚本处理过。
        text.havePropertiesChanged = false;

        // 将当前文本的 RectTransform 暴露给全局配置。
        // 外部脚本可以用它做坐标转换。
        GlobalConfig.rect = text.rectTransform;

        // 重置统计数量。
        GlobalConfig.totalCount = 0;

        // 初始化时清空所有状态列表，避免重复添加旧数据。
        stopList.Clear();
        movingList.Clear();
        spinList.Clear();
        back_MovingList.Clear();
        ChangedMesh.Clear();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // 空格、换行等字符通常没有可见顶点。
            // 这些字符不参与运动逻辑。
            if (!charInfo.isVisible)
            {
                Debug.Log("Not visible: " + i);
                continue;
            }

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 通过字符左下角顶点和右上角顶点求中心点。
            // TMP 一个字符四个顶点中：
            // vertexIndex + 0 通常是左下；
            // vertexIndex + 2 通常是右上。
            Vector2 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;

            // 计算字符原始中心点与当前注视点的距离。
            float distance = Vector2.Distance(center, GlobalConfig.gazePoint);

            // 创建字符运行时数据。
            texts[i] = new Text(materialIndex, vertexIndex, distance, center);

            // 如果初始化时字符就在注视半径内，直接进入 Move 状态。
            if (distance < GlobalConfig.radius)
            {
                // 这里 offset 表示从字符原位指向 gazePoint 的偏移。
                // 这样字符进入运动时，会和当前注视点产生关联。
                center = GlobalConfig.gazePoint - center;

                texts[i].offset = center;
                texts[i].angle = Mathf.Atan2(center.y, center.x);
                texts[i].status = Text_Status.Move;

                movingList.Add(i);
            }
            else
            {
                stopList.Add(i);
            }

            GlobalConfig.totalCount++;
        }

        return texts;
    }

    /// <summary>
    /// Move 状态运动逻辑。
    /// 
    /// 字符会一边旋转，一边逐渐远离原始位置。
    /// 当 distance 达到 orbit_radius 后，会在 Update 中切换到 Spin 状态。
    /// </summary>
    /// <param name="angle">字符当前角度，引用传入，会被持续增加</param>
    /// <param name="distance">字符当前扰动半径，引用传入，会被持续增加</param>
    /// <returns>当前帧根据角度和半径计算出的偏移量</returns>
    private Vector2 Move_Logic(ref float angle, ref float distance)
    {
        angle += move_rotateSpeed * Time.deltaTime;

        distance += GlobalConfig.radialSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, 0f, GlobalConfig.orbit_radius);

        Vector2 offset;
        offset.x = Mathf.Cos(angle) * distance;
        offset.y = Mathf.Sin(angle) * distance;

        return offset;
    }

    /// <summary>
    /// Spin 状态运动逻辑。
    /// 
    /// 字符保持在 orbit_radius 半径上持续旋转。
    /// 与 Move 不同，Spin 不再增加 distance，只更新 angle。
    /// </summary>
    /// <param name="angle">字符当前角度，引用传入，会被持续增加</param>
    /// <returns>当前帧根据角度和固定轨道半径计算出的偏移量</returns>
    private Vector2 Spin_Logic(ref float angle)
    {
        angle += spin_rotateSpeed * Time.deltaTime;

        Vector2 offset;
        offset.x = Mathf.Cos(angle) * GlobalConfig.orbit_radius;
        offset.y = Mathf.Sin(angle) * GlobalConfig.orbit_radius;

        return offset;
    }

    /// <summary>
    /// Back 状态回归逻辑。
    /// 
    /// 字符会逐渐减小 angle 和 distance。
    /// 当 distance 接近 0 时，字符回到原位，并在 Update 中切换到 Stop 状态。
    /// </summary>
    /// <param name="angle">当前角度，引用传入，会逐渐减少</param>
    /// <param name="distance">当前扰动距离，引用传入，会逐渐减少</param>
    /// <param name="rotateSpeed">回归角速度</param>
    /// <param name="radialSpeed">回归半径速度</param>
    /// <returns>当前帧回归状态下的偏移量</returns>
    private Vector2 Back_Logic(ref float angle, ref float distance, float rotateSpeed, float radialSpeed)
    {
        angle -= rotateSpeed * Time.deltaTime;

        distance -= radialSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, 0f, GlobalConfig.orbit_radius);

        // 避免因为浮点误差导致字符永远无法精确回到 0。
        if (distance < 0.01f)
        {
            distance = 0f;
            angle = 0f;
        }

        Vector2 offset;
        offset.x = Mathf.Cos(angle) * distance;
        offset.y = Mathf.Sin(angle) * distance;

        return offset;
    }

    /// <summary>
    /// 根据当前 gazePoint 更新字符状态。
    /// 
    /// 状态切换规则：
    /// 
    /// Stop -> Move:
    /// 字符原始中心点进入注视半径。
    /// 
    /// Move -> Back:
    /// 字符原始中心点离开注视半径。
    /// 
    /// Spin -> Back:
    /// 字符原始中心点离开注视半径。
    /// 
    /// Back -> Move:
    /// 字符正在回归时，如果注视点又靠近它，则重新进入 Move。
    /// 
    /// 注意：
    /// 当前判断使用的是 origCenter，而不是当前扰动后的显示位置。
    /// 这样可以保证检测逻辑基于文字原始排版位置，而不是被扰动后的临时位置。
    /// </summary>
    /// <param name="texts">字符数据数组</param>
    private void ChangeStatus(ref Text[] texts)
    {
        // Stop -> Move
        for (int i = stopList.Count - 1; i >= 0; i--)
        {
            int id = stopList[i];

            float distance = Vector2.Distance(texts[id].origCenter, GlobalConfig.gazePoint);

            if (distance < GlobalConfig.radius)
            {
                texts[id].distance = 0f;

                Vector2 offset = GlobalConfig.gazePoint - texts[id].origCenter;

                texts[id].angle = Mathf.Atan2(offset.y, offset.x);
                texts[id].offset = offset;
                texts[id].status = Text_Status.Move;

                movingList.Add(id);
                stopList.RemoveAt(i);
            }
        }

        // Move -> Back
        for (int i = movingList.Count - 1; i >= 0; i--)
        {
            int id = movingList[i];

            float distance = Vector2.Distance(texts[id].origCenter, GlobalConfig.gazePoint);

            if (distance > GlobalConfig.radius)
            {
                texts[id].status = Text_Status.Back;

                back_MovingList.Add(id);
                movingList.RemoveAt(i);
            }
        }

        // Spin -> Back
        for (int i = spinList.Count - 1; i >= 0; i--)
        {
            int id = spinList[i];

            float distance = Vector2.Distance(texts[id].origCenter, GlobalConfig.gazePoint);

            if (distance > GlobalConfig.radius)
            {
                texts[id].distance = GlobalConfig.orbit_radius;
                texts[id].status = Text_Status.Back;

                back_MovingList.Add(id);
                spinList.RemoveAt(i);
            }
        }

        // Back -> Move
        for (int i = back_MovingList.Count - 1; i >= 0; i--)
        {
            int id = back_MovingList[i];

            float distance = Vector2.Distance(texts[id].origCenter, GlobalConfig.gazePoint);

            if (distance < GlobalConfig.radius)
            {
                texts[id].status = Text_Status.Move;

                movingList.Add(id);
                back_MovingList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 将当前 TMP 顶点恢复到原始顶点状态。
    /// 
    /// 为什么每帧都要恢复？
    /// 因为本脚本修改的是 mesh 顶点。
    /// 如果直接在上一帧的顶点基础上继续加偏移，偏移会不断累积。
    /// 
    /// 正确做法：
    /// 每帧先恢复原始顶点，
    /// 再根据当前状态重新计算本帧应该显示的位置。
    /// </summary>
    private void ResetVertices()
    {
        for (int i = 0; i < origMeshInfo.Length; i++)
        {
            Vector3[] origVertices = origMeshInfo[i].vertices;
            Vector3[] currVertices = textInfo.meshInfo[i].vertices;

            System.Array.Copy(origVertices, currVertices, origVertices.Length);
        }
    }

    /// <summary>
    /// 处理 Move / Spin 状态下，由注视点移动造成的额外偏移。
    /// 
    /// textsOffset 会累积 GlobalConfig.minus_x / minus_y，
    /// 让字符在注视点移动时产生拖拽、错位、滞后的感觉。
    /// 
    /// offset 是当前运动逻辑算出的基础偏移；
    /// textsOffset 是额外残留偏移；
    /// 最终显示偏移 = offset + textsOffset。
    /// </summary>
    /// <param name="offset">当前运动状态计算出的基础偏移</param>
    /// <param name="textsOffset">字符自身保存的额外偏移残留</param>
    private void ChangeOffset(ref Vector2 offset, ref Vector2 textsOffset)
    {
        // 将外部输入的注视点移动量累积到字符自身偏移中。
        textsOffset.x += GlobalConfig.minus_x;
        textsOffset.y += GlobalConfig.minus_y;

        // 限制 x 方向的偏移释放速度。
        if (textsOffset.x != 0)
        {
            if (GlobalConfig.minus_x > 0)
            {
                if (GlobalConfig.minus_x >= GlobalConfig.offsetSpeed)
                {
                    textsOffset.x = textsOffset.x - GlobalConfig.minus_x + GlobalConfig.offsetSpeed;
                }
            }
            else
            {
                if (GlobalConfig.minus_x <= -GlobalConfig.offsetSpeed)
                {
                    textsOffset.x = textsOffset.x - GlobalConfig.minus_x - GlobalConfig.offsetSpeed;
                }
            }
        }

        // 限制 y 方向的偏移释放速度。
        if (textsOffset.y != 0)
        {
            if (GlobalConfig.minus_y > 0)
            {
                if (GlobalConfig.minus_y >= GlobalConfig.offsetSpeed)
                {
                    textsOffset.y = textsOffset.y - GlobalConfig.minus_y + GlobalConfig.offsetSpeed;
                }
            }
            else
            {
                if (GlobalConfig.minus_y <= -GlobalConfig.offsetSpeed)
                {
                    textsOffset.y = textsOffset.y - GlobalConfig.minus_y - GlobalConfig.offsetSpeed;
                }
            }
        }

        // 将额外偏移叠加到最终偏移上。
        offset += textsOffset;
    }

    /// <summary>
    /// Back 状态下处理额外偏移残留。
    /// 
    /// 与 ChangeOffset 不同：
    /// Back 状态不再继续累积 minus_x / minus_y，
    /// 而是把已有的 textsOffset 逐步归零。
    /// 
    /// 这样字符回归原位时，不会瞬间跳回，而是带有平滑回收效果。
    /// </summary>
    /// <param name="offset">当前回归逻辑计算出的基础偏移</param>
    /// <param name="textsOffset">字符自身保存的额外偏移残留</param>
    private void Back_ChangeOffset(ref Vector2 offset, ref Vector2 textsOffset)
    {
        // x 方向逐渐归零。
        if (textsOffset.x > 0)
        {
            if (textsOffset.x >= GlobalConfig.offsetSpeed)
            {
                textsOffset.x -= GlobalConfig.offsetSpeed;
            }
            else
            {
                textsOffset.x = 0;
            }
        }
        else
        {
            if (textsOffset.x <= -GlobalConfig.offsetSpeed)
            {
                textsOffset.x += GlobalConfig.offsetSpeed;
            }
            else
            {
                textsOffset.x = 0;
            }
        }

        // y 方向逐渐归零。
        if (textsOffset.y > 0)
        {
            if (textsOffset.y >= GlobalConfig.offsetSpeed)
            {
                textsOffset.y -= GlobalConfig.offsetSpeed;
            }
            else
            {
                textsOffset.y = 0;
            }
        }
        else
        {
            if (textsOffset.y <= -GlobalConfig.offsetSpeed)
            {
                textsOffset.y += GlobalConfig.offsetSpeed;
            }
            else
            {
                textsOffset.y = 0;
            }
        }

        // 将尚未完全归零的额外偏移叠加到当前帧偏移中。
        offset += textsOffset;
    }

    /// <summary>
    /// 将计算出的位移、旋转、缩放应用到某一个字符的四个顶点上。
    /// 
    /// 这是实际改变文字显示效果的函数。
    /// 
    /// 处理流程：
    /// 1. 从原始顶点数据中读取字符四个顶点。
    /// 2. 将每个顶点转换到以字符中心为原点的局部坐标。
    /// 3. 对局部坐标应用缩放和旋转。
    /// 4. 再加回字符原始中心点和当前偏移。
    /// 5. 写入当前 mesh 顶点数组。
    /// 6. 记录本帧发生变化的 materialIndex。
    /// </summary>
    /// <param name="vertexIndex">字符顶点起始索引</param>
    /// <param name="materialIndex">字符所在材质索引</param>
    /// <param name="origCenter">字符原始中心点</param>
    /// <param name="angle">字符当前旋转角度，单位弧度</param>
    /// <param name="offset">字符当前总偏移</param>
    /// <param name="distance">字符当前扰动距离，用于计算缩放比例</param>
    private void ApplyOffset(int vertexIndex, int materialIndex, Vector2 origCenter, float angle, Vector2 offset, float distance)
    {
        Vector3[] origVertices = origMeshInfo[materialIndex].vertices;
        Vector3[] currVertices = textInfo.meshInfo[materialIndex].vertices;

        // Unity 的 Quaternion.Euler 使用角度制，所以这里从弧度转回角度。
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

        // 根据字符离开原位的距离计算缩放。
        // 离原位越远，文字越大，视觉扰动越明显。
        float scale = 1f + (distance / GlobalConfig.orbit_radius) * (GlobalConfig.max_extraScale);

        Vector3 offset3D = (Vector3)offset;
        Vector3 origCenter3D = (Vector3)origCenter;

        int index = vertexIndex;

        for (int i = 0; i < 4; i++)
        {
            // 先把顶点变成以字符中心为原点的局部坐标。
            Vector3 localVertex = origVertices[index + i] - origCenter3D;

            // 再对局部顶点做缩放和旋转，最后平移到目标位置。
            currVertices[index + i] = origCenter3D + offset3D + rotation * (scale * localVertex);
        }

        // 标记该材质对应的 mesh 本帧需要更新。
        ChangedMesh.Add(materialIndex);
    }

    /// <summary>
    /// 调试用：每秒输出一次各状态列表中的字符数量。
    /// 
    /// 当前默认没有启用。
    /// 可以在 Update 末尾取消 Debug_judge() 的注释来查看状态变化。
    /// </summary>
    private void Debug_judge()
    {
        debugTimer += Time.deltaTime;

        if (debugTimer >= 1f)
        {
            debugTimer = 0f;

            /*
            float gazeRight = GlobalConfig.gazePoint.x + GlobalConfig.orbit_radius;
            float gazeLeft = GlobalConfig.gazePoint.x - GlobalConfig.orbit_radius;
            float gazeUp = GlobalConfig.gazePoint.y + GlobalConfig.orbit_radius;
            float gazeDown = GlobalConfig.gazePoint.y - GlobalConfig.orbit_radius;

            for (int i = 0; i < 4; i++)
            {
                Debug.Log(texts[i].origCnter + "||" + i + "||" + gazeLeft + "||" + gazeRight + "||" + gazeDown + "||" + gazeUp);
            }
            */

            Debug.Log(stopList.Count + "||" + movingList.Count + "||" + spinList.Count + "||" + back_MovingList.Count);

            // Debug.Log("Gaze Position: " + GlobalConfig.gazePoint + GlobalConfig.minus_x + "||" + GlobalConfig.minus_y);
        }
    }

    /// <summary>
    /// 调试用：统计函数被调用次数。
    /// 当前默认没有启用。
    /// </summary>
    private void Debug_count()
    {
        debugCount++;
        Debug.Log("|||||" + debugCount);
    }

    /// <summary>
    /// Unity 生命周期函数。
    /// 
    /// 场景开始时初始化字符数据。
    /// </summary>
    void Start()
    {
        if (texts == null || texts.Length == 0)
        {
            texts = new Text[textInfo.characterCount];
        }

        texts = InitTextInfo();
    }

    /// <summary>
    /// Unity 生命周期函数。
    /// 
    /// 组件启用时重新初始化字符数据。
    /// 
    /// 注意：
    /// 当前版本中 OnEnable 可能会早于某些外部初始化逻辑执行。
    /// 如果出现 text 为空的问题，可以在这里加 text == null 判断。
    /// </summary>
    private void OnEnable()
    {
        texts = InitTextInfo();
    }

    /// <summary>
    /// Unity 每帧更新函数。
    /// 
    /// 整体流程：
    /// 1. 检查必要数据是否存在。
    /// 2. 如果 TMP 文本属性发生变化，则重新初始化。
    /// 3. 恢复原始顶点，避免顶点偏移累积。
    /// 4. 根据 gazePoint 和 minus_x / minus_y 判断是否需要更新状态。
    /// 5. 分别处理 Move、Spin、Back 三类字符。
    /// 6. 将变化后的顶点提交给 TMP。
    /// </summary>
    void Update()
    {
        // 防止脚本启动顺序或 Inspector 未赋值导致空引用。
        if (text == null || texts == null || origMeshInfo == null)
        {
            return;
        }

        // 如果文本内容、字体、字号、排版等 TMP 属性发生变化，
        // 或者字符数量变化，就重新初始化字符信息。
        if (text.havePropertiesChanged || text.textInfo.characterCount != texts.Length)
        {
            texts = InitTextInfo();
        }

        Vector2 offset;

        textInfo = text.textInfo;

        // 每帧先把所有顶点恢复成原始排版状态。
        ResetVertices();

        // 当注视点移动量足够明显时，才进行状态切换。
        // 这样可以减少小抖动导致的频繁状态变化。
        if (GlobalConfig.minus_x < -3 || GlobalConfig.minus_x > 3 ||
            GlobalConfig.minus_y < -3 || GlobalConfig.minus_y > 3)
        {
            ChangeStatus(ref texts);
        }

        // 处理 Move 状态字符：
        // 字符从原位向外扩散，同时旋转和缩放。
        for (int i = movingList.Count - 1; i >= 0; i--)
        {
            int id = movingList[i];

            offset = Move_Logic(ref texts[id].angle, ref texts[id].distance);

            ChangeOffset(ref offset, ref texts[id].offset);

            ApplyOffset(
                texts[id].vertexIndex,
                texts[id].materialIndex,
                texts[id].origCenter,
                texts[id].angle,
                offset,
                texts[id].distance
            );

            // 当字符到达最大扰动半径后，切换到 Spin 状态。
            if (texts[id].distance >= GlobalConfig.orbit_radius)
            {
                texts[id].distance = GlobalConfig.orbit_radius;
                texts[id].status = Text_Status.Spin;

                spinList.Add(id);
                movingList.RemoveAt(i);
            }
        }

        // 处理 Spin 状态字符：
        // 字符保持最大半径持续旋转。
        for (int i = spinList.Count - 1; i >= 0; i--)
        {
            int id = spinList[i];

            offset = Spin_Logic(ref texts[id].angle);

            ChangeOffset(ref offset, ref texts[id].offset);

            ApplyOffset(
                texts[id].vertexIndex,
                texts[id].materialIndex,
                texts[id].origCenter,
                texts[id].angle,
                offset,
                texts[id].distance
            );
        }

        // 处理 Back 状态字符：
        // 字符逐渐回归原始排版位置。
        for (int i = back_MovingList.Count - 1; i >= 0; i--)
        {
            int id = back_MovingList[i];

            offset = Back_Logic(
                ref texts[id].angle,
                ref texts[id].distance,
                texts[id].back_angleSpeed,
                texts[id].back_radiusSpeed
            );

            Back_ChangeOffset(ref offset, ref texts[id].offset);

            ApplyOffset(
                texts[id].vertexIndex,
                texts[id].materialIndex,
                texts[id].origCenter,
                texts[id].angle,
                offset,
                texts[id].distance
            );

            // 当扰动距离足够接近 0，认为字符已经回到原位。
            if (texts[id].distance < 0.01f)
            {
                texts[id].angle = 0f;
                texts[id].distance = 0f;
                texts[id].offset = new(0, 0);
                texts[id].status = Text_Status.Stop;

                stopList.Add(id);
                back_MovingList.RemoveAt(i);
            }
        }

        // 将本帧修改过的 mesh 顶点提交给 TMP。
        foreach (int matIndex in ChangedMesh)
        {
            TMP_MeshInfo meshInfo = text.textInfo.meshInfo[matIndex];

            meshInfo.mesh.vertices = meshInfo.vertices;

            text.UpdateGeometry(meshInfo.mesh, matIndex);

            // 通知 TMP 顶点数据发生变化。
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        // 清空本帧变化记录，等待下一帧重新收集。
        ChangedMesh.Clear();

        // 调试时可以打开。
        // Debug_judge();
    }
}