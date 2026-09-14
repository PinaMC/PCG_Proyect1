using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystemTreeGeneratorP1 : MonoBehaviour
{
    public enum GenerationMode
    {
        TwoD,
        ThreeD
    }


    // -------------------------------------------------------------------------
    // CONFIGURACIÓN
    // -------------------------------------------------------------------------

    [Header("Generation")]

    [SerializeField]
    private GenerationMode generationMode =
        GenerationMode.TwoD;

    [SerializeField]
    private bool autoUpdate = true;


    [Header("2D Configuration")]

    [SerializeField]
    private string axiom2D = "F";

    [SerializeField]
    private List<LSystemRule> rules2D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[+F]F[-F]F"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations2D = 3;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle2D = 25f;


    [Header("3D Configuration")]

    [SerializeField]
    private string axiom3D = "F";

    // Regla pensada para pino: verticilo de 3 ramas (pitch + roll de 120°)
    // seguido de continuación de tronco. Con angle3D = 40, "///" acumula
    // 3 x 40 = 120 grados de roll entre cada rama del verticilo.
    [SerializeField]
    private List<LSystemRule> rules3D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[&F]///[&F]///[&F]///F"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations3D = 5;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle3D = 40f;


    [Header("Shared Visualization")]

    [Min(0.05f)]
    [SerializeField]
    private float segmentLength = 1f;

    [Min(0.005f)]
    [SerializeField]
    private float branchRadius = 0.06f;

    [SerializeField]
    private Material branchMaterial;


    [Header("Height Color")]

    [SerializeField]
    private bool useHeightColor = true;

    [SerializeField]
    private Color lowColor =new Color(0.35f,0.17f,0.06f,1f);

    [SerializeField]
    private Color highColor =new Color(0.15f,0.55f,0.18f,1f);

    [Min(0.1f)]
    [SerializeField]
    private float colorHeight = 8f;


    [Header("Debug")]

    [SerializeField]
    private bool showDerivation = true;


    [HideInInspector]
    [SerializeField]
    private Transform generatedRoot;

    private int branchCounter = 0;

   
    public void SetIterations(int iteraciones)
    {
        if (generationMode == GenerationMode.ThreeD)
        {
            iterations3D = Mathf.Clamp(iteraciones, 0, 5);
        }
        else
        {
            iterations2D = Mathf.Clamp(iteraciones, 0, 5);
        }
    }

    public int GetIterations()
    {
        return generationMode == GenerationMode.ThreeD ? iterations3D : iterations2D;
    }

    public void SetAngle(float angle)
    {
        if (generationMode == GenerationMode.ThreeD)
        {
            angle3D = Mathf.Clamp(angle, 1f, 90f);
        }
        else
        {
            angle2D = Mathf.Clamp(angle, 1f, 90f);
        }
    }

    public float GetAngle()
    {
        return generationMode == GenerationMode.ThreeD ? angle3D : angle2D;
    }

    public void SetSegmentLength(float length)
    {
        segmentLength = Mathf.Max(0.05f, length);
    }

    public float GetSegmentLength()
    {
        return segmentLength;
    }

    public void SetBranchRadius(float radius)
    {
        branchRadius = Mathf.Max(0.005f, radius);
    }

    public float GetBranchRadius()
    {
        return branchRadius;
    }

    public void SetGenerationMode(GenerationMode mode)
    {
        generationMode = mode;
    }

    public GenerationMode GetGenerationMode()
    {
        return generationMode;
    }


    // -------------------------------------------------------------------------
    // ESTADO DE LA TORTUGA
    // -------------------------------------------------------------------------
    private struct TurtleState
    {
        public Vector3 position;
        public Quaternion rotation;

        public TurtleState(
            Vector3 position,
            Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }


    // -------------------------------------------------------------------------
    // GENERACIÓN DEL L-SYSTEM
    // -------------------------------------------------------------------------
    public void GenerateTree()
    {
        ClearGeneratedTree();
        EnsureGeneratedRoot();

        branchCounter = 0;

        string activeAxiom;
        List<LSystemRule> activeRules;
        int activeIterations;
        float activeAngle;


        if (generationMode ==
            GenerationMode.TwoD)
        {
            activeAxiom =
                axiom2D;

            activeRules =
                rules2D;

            activeIterations =
                iterations2D;

            activeAngle =
                angle2D;
        }
        else
        {
            activeAxiom =
                axiom3D;

            activeRules =
                rules3D;

            activeIterations =
                iterations3D;

            activeAngle =
                angle3D;
        }


        List<string> derivation =
            new List<string>();


        string sequence =
            ParallelGrammarGenerator.Generate(
                activeAxiom,
                activeRules,
                activeIterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                activeAxiom,
                activeRules,
                derivation,
                sequence
            );
        }


        Interpret(
            sequence,
            activeAngle
        );
    }


    // -------------------------------------------------------------------------
    // TURTLE GRAPHICS
    // -------------------------------------------------------------------------
    //
    // NOTA DE LA CORRECCIÓN (& ^ vs \ /):
    //
    // El heading de la tortuga es ActualRotation * Vector3.up. Rotar alrededor
    // de ese mismo eje (Vector3.up) NO cambia el heading — por eso & y ^ deben
    // usar un eje perpendicular (Vector3.right) para producir pitch real
    // (inclinar la rama arriba/abajo). \ y / en cambio deben rotar alrededor
    // del propio heading (Vector3.up) — eso es roll: no desvía la rama actual,
    // pero reorienta el marco de la tortuga para que la SIGUIENTE ramificación
    // salga en otro ángulo alrededor del tronco (así se logran los verticilos
    // simétricos de un pino usando "///").
    //
    private void Interpret(
        string sequence,
        float activeAngle)
    {
        Vector3 ActualPosition = Vector3.zero;
        Quaternion ActualRotation = Quaternion.identity;
        Stack<TurtleState> Tree = new Stack<TurtleState>();

        foreach (char ch in sequence)
        {
            Vector3 Direccion = ActualRotation * Vector3.up;

            switch (ch)
            {
                case 'F':
                    Vector3 InicioPosicion = ActualPosition;

                    ActualPosition = ActualPosition + Direccion * segmentLength;
                    CreateBranch(InicioPosicion, ActualPosition);
                    break;

                case 'f':
                    ActualPosition = ActualPosition + Direccion * segmentLength;
                    break;

                case '+':
                    ActualRotation *= Quaternion.AngleAxis(activeAngle, Vector3.forward);
                    break;

                case '-':
                    ActualRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.forward);
                    break;

                case '[':
                    Tree.Push(new TurtleState(ActualPosition, ActualRotation));
                    break;

                case ']':
                    TurtleState prev = Tree.Pop();
                    ActualPosition = prev.position;
                    ActualRotation = prev.rotation;
                    break;

                // Pitch real: eje perpendicular al heading (Vector3.right)
                case '&':
                    if (generationMode == GenerationMode.ThreeD)
                    {
                        ActualRotation *= Quaternion.AngleAxis(activeAngle, Vector3.right);
                    }
                    break;

                case '^':
                    if (generationMode == GenerationMode.ThreeD)
                    {
                        ActualRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.right);
                    }
                    break;

                // Roll real: eje = el propio heading (Vector3.up)
                case '\\':
                    if (generationMode == GenerationMode.ThreeD)
                    {
                        ActualRotation *= Quaternion.AngleAxis(activeAngle, Vector3.up);
                    }
                    break;

                case '/':
                    if (generationMode == GenerationMode.ThreeD)
                    {
                        ActualRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.up);
                    }
                    break;
            }
        }
    }


    // -------------------------------------------------------------------------
    // CREACIÓN DE SEGMENTOS
    // -------------------------------------------------------------------------
    private void CreateBranch(
        Vector3 start,
        Vector3 end)
    {
        Vector3 direction =
            end - start;

        float length =
            direction.magnitude;

        if (length <= 0f)
        {
            return;
        }


        GameObject branch =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        branchCounter++;

        branch.name =
            "Branch_" +
            branchCounter.ToString("0000");


        branch.transform.SetParent(
            generatedRoot,
            false
        );


        Vector3 middlePoint =
            (start + end) *
            0.5f;


        branch.transform.localPosition =
            middlePoint;


        branch.transform.localRotation =
            Quaternion.FromToRotation(
                Vector3.up,
                direction.normalized
            );


        branch.transform.localScale =
            new Vector3(
                branchRadius,
                length * 0.5f,
                branchRadius
            );


        Renderer renderer =
            branch.GetComponent<Renderer>();


        if (renderer != null &&
            branchMaterial != null)
        {
            renderer.sharedMaterial =
                branchMaterial;
        }


        if (renderer != null &&
            useHeightColor)
        {
            ApplyHeightColor(
                renderer,
                middlePoint.y
            );
        }


        Collider collider =
            branch.GetComponent<Collider>();

        if (collider != null)
        {
            SafeDestroy(
                collider
            );
        }
    }


    // -------------------------------------------------------------------------
    // COLOR SEGÚN ALTURA
    // -------------------------------------------------------------------------
    private void ApplyHeightColor(
        Renderer renderer,
        float height)
    {
        float safeHeight =
            Mathf.Max(
                0.1f,
                colorHeight
            );


        float t =
            Mathf.InverseLerp(
                0f,
                safeHeight,
                height
            );


        Color branchColor =
            Color.Lerp(
                lowColor,
                highColor,
                t
            );


        MaterialPropertyBlock block =
            new MaterialPropertyBlock();


        renderer.GetPropertyBlock(
            block
        );


        Material material =
            renderer.sharedMaterial;


        if (material != null)
        {
            if (material.HasProperty(
                "_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    branchColor
                );
            }


            if (material.HasProperty(
                "_Color"))
            {
                block.SetColor(
                    "_Color",
                    branchColor
                );
            }
        }


        renderer.SetPropertyBlock(
            block
        );
    }


    // -------------------------------------------------------------------------
    // INFRAESTRUCTURA DE VISUALIZACIÓN
    // -------------------------------------------------------------------------

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
        {
            return;
        }


        Transform existing =
            transform.Find(
                "Generated Tree"
            );


        if (existing != null)
        {
            generatedRoot =
                existing;

            return;
        }


        GameObject root =
            new GameObject(
                "Generated Tree"
            );


        root.transform.SetParent(
            transform,
            false
        );


        root.transform.localPosition =
            Vector3.zero;

        root.transform.localRotation =
            Quaternion.identity;

        root.transform.localScale =
            Vector3.one;


        generatedRoot =
            root.transform;
    }


    public void ClearGeneratedTree()
    {
        if (generatedRoot == null)
        {
            Transform existing =
                transform.Find(
                    "Generated Tree"
                );


            if (existing != null)
            {
                generatedRoot =
                    existing;
            }
        }


        if (generatedRoot != null)
        {
            SafeDestroy(
                generatedRoot.gameObject
            );

            generatedRoot =
                null;
        }


        branchCounter = 0;
    }


    // -------------------------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------------------------

    private void PrintDerivation(
        string activeAxiom,
        List<LSystemRule> activeRules,
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== L-SYSTEM TREE ====="
        );


        output.AppendLine();

        output.AppendLine(
            "MODE:"
        );

        output.AppendLine(
            generationMode.ToString()
        );


        output.AppendLine();

        output.AppendLine(
            "AXIOM:"
        );

        output.AppendLine(
            activeAxiom
        );


        output.AppendLine();

        output.AppendLine(
            "RULES:"
        );


        foreach (LSystemRule rule in activeRules)
        {
            if (rule == null)
            {
                continue;
            }


            output.AppendLine(
                rule.predecessor +
                " -> " +
                rule.successor
            );
        }


        output.AppendLine();

        output.AppendLine(
            "DERIVATION:"
        );


        for (int i = 0;
             i < derivation.Count;
             i++)
        {
            output.AppendLine(
                "Iteration " +
                i +
                ": " +
                derivation[i]
            );
        }


        output.AppendLine();

        output.AppendLine(
            "FINAL STRING:"
        );

        output.AppendLine(
            finalSequence
        );


        Debug.Log(
            output.ToString(),
            this
        );
    }


    private void SafeDestroy(
        UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }


        if (Application.isPlaying)
        {
            Destroy(
                target
            );
        }
        else
        {
            DestroyImmediate(
                target
            );
        }
    }
}
