using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    // Drop this on an empty GameObject.
    // Controls:
    // - Left click: paint selected ink
    // - 1/2/3: choose ink
    // - Space: advance 1 turn
    // - R: reset
    public class InkSandboxHarness : MonoBehaviour
    {
        [Header("Grid")]
        public int width = 32;
        public int height = 18;
        public float cellSize = 0.5f;

        [Header("Default saturation")]
        public float saturationStone = 2.0f;
        public float saturationSoil  = 3.0f;
        public float saturationFlesh = 1.6f;
        public float saturationMetal = 1.2f;

        [Header("Ink Recipes (optional; if empty, runtime defaults are created)")]
        public InkRecipe ink1;
        public InkRecipe ink2;
        public InkRecipe ink3;

        [Header("Painting")]
        public float paintAmountPerSecond = 6.0f;

        [Header("Rendering")]
        public bool useGizmos = false;
        private InkGridRenderer _renderer;


        private InkGrid _grid;
        private InkSimulator _sim;
        private InkRecipe _selected;

        private InkRecipe _runtime1;
        private InkRecipe _runtime2;
        private InkRecipe _runtime3;

        private void OnEnable()
        {
            EnsureCamera();
            Build();
        }

private void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.transform.position = new Vector3(width * cellSize * 0.5f, height * cellSize * 0.5f, -10f);
            cam.orthographicSize = Mathf.Max(2f, height * cellSize * 0.6f);
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f); // Dark background
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

private void Build()
        {
            InkRecipe a = (ink1 != null) ? ink1 : CreateRuntimeInk1();
            InkRecipe b = (ink2 != null) ? ink2 : CreateRuntimeInk2();
            InkRecipe c = (ink3 != null) ? ink3 : CreateRuntimeInk3();

            _selected = a;

            _grid = new InkGrid(width, height, (x, y) =>
            {
                // Simple substrate bands (we'll replace with real world data later)
                InkSubstrate sub = (y < height / 3) ? InkSubstrate.Stone : (y < 2 * height / 3) ? InkSubstrate.Soil : InkSubstrate.Metal;
                float sat = 2f;
                if (sub == InkSubstrate.Stone) sat = saturationStone;
                else if (sub == InkSubstrate.Soil) sat = saturationSoil;
                else if (sub == InkSubstrate.Flesh) sat = saturationFlesh;
                else if (sub == InkSubstrate.Metal) sat = saturationMetal;
                return new InkCell(sub, sat);
            });

            _sim = new InkSimulator(_grid);
            InkTurnSystem.Reset(0);

            // Initialize sprite-based renderer
            if (!useGizmos)
            {
                _renderer = GetComponent<InkGridRenderer>();
                if (_renderer == null)
                    _renderer = gameObject.AddComponent<InkGridRenderer>();
                _renderer.Initialize(_grid, cellSize);
            }
        }

private void Update()
        {
            if (_grid == null || _sim == null) Build();

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null) return;

            bool needsVisualUpdate = false;

            // Ink selection
            if (kb.digit1Key.wasPressedThisFrame) _selected = (ink1 != null) ? ink1 : _runtime1;
            if (kb.digit2Key.wasPressedThisFrame) _selected = (ink2 != null) ? ink2 : _runtime2;
            if (kb.digit3Key.wasPressedThisFrame) _selected = (ink3 != null) ? ink3 : _runtime3;

            // Advance turn
            if (kb.spaceKey.wasPressedThisFrame)
            {
                InkTurnSystem.Advance();
                _sim.Tick();
                needsVisualUpdate = true;
            }

            // Reset
            if (kb.rKey.wasPressedThisFrame) Build();

            // Paint ink
            if (mouse != null && mouse.leftButton.isPressed && _selected != null)
            {
                var m = MouseToGrid(mouse);
                if (m.ok)
                {
                    _sim.ApplyInk(m.x, m.y, _selected, paintAmountPerSecond * Time.deltaTime);
                    needsVisualUpdate = true;
                }
            }

            // Update renderer
            if (needsVisualUpdate && _renderer != null)
            {
                _renderer.UpdateVisuals();
            }
        }

        private (int x, int y, bool ok) MouseToGrid(Mouse mouse)
        {
            Camera cam = Camera.main;
            if (cam == null) return (0, 0, false);
            
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
            Vector3 local = w - transform.position;
            int x = Mathf.FloorToInt(local.x / cellSize);
            int y = Mathf.FloorToInt(local.y / cellSize);
            return (x, y, _grid != null && _grid.InBounds(x, y));
        }

private void OnDrawGizmos()
        {
            if (!useGizmos || _grid == null) return;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);

            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    float total = cell.TotalInk();

                    Color baseC;
                    if (cell.substrate == InkSubstrate.Stone) baseC = new Color(0.25f, 0.25f, 0.28f, 1f);
                    else if (cell.substrate == InkSubstrate.Soil) baseC = new Color(0.22f, 0.18f, 0.10f, 1f);
                    else if (cell.substrate == InkSubstrate.Flesh) baseC = new Color(0.28f, 0.15f, 0.15f, 1f);
                    else baseC = new Color(0.18f, 0.20f, 0.24f, 1f);

                    Color inkC = Color.clear;
                    InkLayer dominant = InkDominance.GetDominant(cell);
                    if (dominant != null && dominant.recipe != null)
                        inkC = dominant.recipe.uiColor;

                    float t = Mathf.Clamp01(total / Mathf.Max(0.0001f, cell.saturationLimit));
                    Gizmos.color = Color.Lerp(baseC, inkC, t);

                    Vector3 center = new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
                    Vector3 size = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.01f);
                    Gizmos.DrawCube(center, size);
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 280, 120));
            GUILayout.Box($"Turn: {InkTurnSystem.Turn}");
            if (_selected != null)
                GUILayout.Box($"Selected: {_selected.displayName}");
            GUILayout.Label("[1/2/3] select ink | [Space] tick | [R] reset");
            GUILayout.Label("[Left click] paint ink");
            GUILayout.EndArea();
        }

        private InkRecipe CreateRuntimeInk1()
        {
            if (_runtime1 != null) return _runtime1;
            _runtime1 = ScriptableObject.CreateInstance<InkRecipe>();
            _runtime1.id = "binding_black_gel";
            _runtime1.displayName = "Binding (Black Gel)";
            _runtime1.domain = InkDomain.Binding;
            _runtime1.carrier = InkCarrier.Gel;
            _runtime1.additives = InkAdditives.Resin | InkAdditives.Thickener;
            _runtime1.viscosity = 2.5f;
            _runtime1.volatility = 0.05f;
            _runtime1.spreadRate = 0.6f;
            _runtime1.uiColor = new Color(0.10f, 0.10f, 0.12f, 1f);
            return _runtime1;
        }

        private InkRecipe CreateRuntimeInk2()
        {
            if (_runtime2 != null) return _runtime2;
            _runtime2 = ScriptableObject.CreateInstance<InkRecipe>();
            _runtime2.id = "growth_green_water";
            _runtime2.displayName = "Growth (Green Water)";
            _runtime2.domain = InkDomain.Growth;
            _runtime2.carrier = InkCarrier.Water;
            _runtime2.additives = InkAdditives.Surfactant | InkAdditives.Spores;
            _runtime2.viscosity = 0.9f;
            _runtime2.volatility = 0.10f;
            _runtime2.spreadRate = 1.2f;
            _runtime2.uiColor = new Color(0.20f, 0.85f, 0.30f, 1f);
            return _runtime2;
        }

        private InkRecipe CreateRuntimeInk3()
        {
            if (_runtime3 != null) return _runtime3;
            _runtime3 = ScriptableObject.CreateInstance<InkRecipe>();
            _runtime3.id = "decay_yellow_solvent";
            _runtime3.displayName = "Decay (Yellow Solvent)";
            _runtime3.domain = InkDomain.Decay;
            _runtime3.carrier = InkCarrier.Solvent;
            _runtime3.additives = InkAdditives.Catalyst;
            _runtime3.viscosity = 0.6f;
            _runtime3.volatility = 0.35f;
            _runtime3.spreadRate = 1.0f;
            _runtime3.uiColor = new Color(0.95f, 0.90f, 0.20f, 1f);
            return _runtime3;
        }
    }
}
