using UnityEngine;

public class VRPrimitiveArm : MonoBehaviour
{
    [Header("Sleeve (jacket)")]
    [SerializeField] Color sleeveColor = new Color(0.08f, 0.08f, 0.12f);
    [SerializeField] float upperArmRadius = 0.07f;
    [SerializeField] float upperArmLength = 0.28f;
    [SerializeField] float elbowRadius    = 0.065f;
    [SerializeField] float forearmRadius  = 0.05f;
    [SerializeField] float forearmLength  = 0.24f;
    [SerializeField] float cuffRadius     = 0.068f;
    [SerializeField] float cuffLength     = 0.06f;

    [Header("Hand")]
    [SerializeField] Color handColor = new Color(0.75f, 0.65f, 0.55f);
    [SerializeField] float palmWidth  = 0.075f;
    [SerializeField] float palmHeight = 0.04f;
    [SerializeField] float palmDepth  = 0.09f;

    [Header("Fingers")]
    [SerializeField] float fingerRadius    = 0.009f;
    [SerializeField] float proximalLength  = 0.032f;
    [SerializeField] float middleLength    = 0.025f;
    [SerializeField] float distalLength    = 0.018f;

    public Transform UpperArmTf { get; private set; }
    public Transform ElbowTf    { get; private set; }
    public Transform ForearmTf  { get; private set; }
    public Transform CuffTf     { get; private set; }
    public Transform HandRootTf { get; private set; }

    // fingerBones[finger][joint]: finger 0=thumb…4=pinky, joint 0=proximal 1=middle 2=distal
    public Transform[,] FingerBones { get; private set; }

    static readonly Vector3[] FingerPalmOffsets = new Vector3[]
    {
        new Vector3(-0.033f, 0f,  0.035f), // thumb
        new Vector3(-0.018f, 0f,  0.045f), // index
        new Vector3(-0.006f, 0f,  0.048f), // middle
        new Vector3( 0.006f, 0f,  0.045f), // ring
        new Vector3( 0.018f, 0f,  0.040f), // pinky
    };

    Material _sleeveMat;
    Material _handMat;

    void Awake()
    {
        _sleeveMat = CreateMat(sleeveColor);
        _handMat   = CreateMat(handColor);

        UpperArmTf = CreateCapsule("UpperArm", upperArmRadius, upperArmLength, _sleeveMat);
        ElbowTf    = CreateSphere("Elbow",     elbowRadius,                    _sleeveMat);
        ForearmTf  = CreateCapsule("Forearm",  forearmRadius, forearmLength,   _sleeveMat);
        CuffTf     = CreateCapsule("Cuff",     cuffRadius,    cuffLength,      _sleeveMat);

        HandRootTf = new GameObject("HandRoot").transform;
        HandRootTf.SetParent(transform);
        CreateBox("Palm", new Vector3(palmWidth, palmHeight, palmDepth), _handMat, HandRootTf);

        FingerBones = new Transform[5, 3];
        for (int f = 0; f < 5; f++)
        {
            var fingerRoot = new GameObject($"Finger{f}").transform;
            fingerRoot.SetParent(HandRootTf);
            fingerRoot.localPosition = FingerPalmOffsets[f];
            fingerRoot.localRotation = Quaternion.identity;

            float[] lengths = { proximalLength, middleLength, distalLength };
            Transform prev = fingerRoot;
            for (int j = 0; j < 3; j++)
            {
                var bone = new GameObject($"Finger{f}_J{j}").transform;
                bone.SetParent(prev);
                bone.localPosition = j == 0 ? Vector3.zero : new Vector3(0f, 0f, lengths[j - 1]);
                bone.localRotation = Quaternion.identity;
                // Capsule extends along Y by default; rotate 90° so it extends along Z (finger direction)
                var seg = CreateCapsule($"Seg{j}", fingerRadius, lengths[j], _handMat, bone);
                seg.localRotation = Quaternion.Euler(90f, 0f, 0f);
                seg.localPosition = new Vector3(0f, 0f, lengths[j] * 0.5f);
                FingerBones[f, j] = bone;
                prev = bone;
            }
        }
    }

    Transform CreateCapsule(string goName, float radius, float height, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = goName;
        go.transform.SetParent(parent != null ? parent : transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        Destroy(go.GetComponent<CapsuleCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    Transform CreateSphere(string goName, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = goName;
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * radius * 2f;
        Destroy(go.GetComponent<SphereCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    void CreateBox(string goName, Vector3 size, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = size;
        Destroy(go.GetComponent<BoxCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material CreateMat(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.08f);
        return mat;
    }
}
