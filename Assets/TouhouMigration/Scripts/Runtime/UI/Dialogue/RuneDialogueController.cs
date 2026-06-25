using System;
using TouhouMigration.Runtime.Dialogue;
using UnityEngine;

namespace TouhouMigration.Runtime.UI.Dialogue
{
    public sealed class RuneDialogueController : MonoBehaviour
    {
        private DialogueRuntimeFacade facade;
        private DialogueViewModel currentModel = new DialogueViewModel();
        private readonly MigrationPortraitCatalog portraits = new MigrationPortraitCatalog();
        private GUIStyle shadeStyle;
        private GUIStyle panelStyle;
        private GUIStyle nameplateStyle;
        private GUIStyle speakerStyle;
        private GUIStyle textStyle;
        private GUIStyle choiceStyle;
        private GUIStyle selectedChoiceStyle;
        private float visibleCharacters;
        private int selectedChoiceIndex;

        public event Action<int> AdvanceRequested;
        public event Action<int, DialogueChoice, int> ChoiceSelected;

        public float TypewriterCharsPerSecond { get; set; } = 34f;
        public bool IsVisible { get; private set; }
        public bool IsTyping { get; private set; }
        public string SpeakerText { get; private set; } = string.Empty;
        public string FullText { get; private set; } = string.Empty;
        public string VisibleText => IsTyping ? FullText.Substring(0, Mathf.Clamp((int)visibleCharacters, 0, FullText.Length)) : FullText;
        public int ChoiceCount => currentModel.Choices.Count;
        public int SessionId => currentModel.SessionId;
        public string PortraitExpression => currentModel.Expression;
        public string PortraitMotion { get; private set; } = "hidden";
        // E5 portrait slot: the Resources key + loaded texture for the current speaker. Texture stays null
        // until the generated portrait art exists (Codex/image2 fills Resources/Portraits/<npc>/<expr>.png).
        public string PortraitResourceKey { get; private set; }
        public Texture2D PortraitTexture { get; private set; }

        public void Bind(DialogueRuntimeFacade dialogueFacade)
        {
            if (facade != null)
            {
                facade.ViewModelChanged -= ShowViewModel;
            }

            facade = dialogueFacade;
            if (facade != null)
            {
                facade.ViewModelChanged += ShowViewModel;
            }
        }

        public void ShowViewModel(DialogueViewModel model)
        {
            currentModel = model ?? new DialogueViewModel();
            if (!currentModel.Active)
            {
                HideDialogue();
                return;
            }

            IsVisible = true;
            SpeakerText = string.IsNullOrWhiteSpace(currentModel.Speaker) ? "？" : currentModel.Speaker;
            FullText = currentModel.Text ?? string.Empty;
            visibleCharacters = 0f;
            IsTyping = FullText.Length > 0;
            selectedChoiceIndex = 0;
            PortraitMotion = ResolvePortraitMotion(currentModel.Expression);
            UpdatePortrait();
        }

        // Resolve the portrait slot for the current speaker. A narration line (blank speaker) clears it;
        // otherwise the texture is loaded from Resources (null until the art is generated).
        private void UpdatePortrait()
        {
            if (portraits.IsNarration(currentModel.Speaker))
            {
                PortraitResourceKey = null;
                PortraitTexture = null;
                return;
            }

            PortraitResourceKey = portraits.ResolveResourceKey(currentModel.NpcId, currentModel.Expression);
            PortraitTexture = portraits.LoadPortrait(currentModel.NpcId, currentModel.Expression);
        }

        public void HideDialogue()
        {
            IsVisible = false;
            IsTyping = false;
            PortraitMotion = "hidden";
            PortraitResourceKey = null;
            PortraitTexture = null;
        }

        public void CompleteTypewriter()
        {
            IsTyping = false;
            visibleCharacters = FullText.Length;
        }

        public bool ConfirmChoice(int choiceIndex)
        {
            if (IsTyping)
            {
                CompleteTypewriter();
                return false;
            }

            if (choiceIndex < 0 || choiceIndex >= currentModel.Choices.Count)
            {
                return false;
            }

            DialogueChoice choice = currentModel.Choices[choiceIndex].Clone();
            ChoiceSelected?.Invoke(choiceIndex, choice, currentModel.SessionId);
            facade?.ChooseForSession(currentModel.SessionId, choiceIndex);
            return true;
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            if (IsTyping)
            {
                visibleCharacters = Mathf.Min(FullText.Length, visibleCharacters + TypewriterCharsPerSecond * Time.unscaledDeltaTime);
                if ((int)visibleCharacters >= FullText.Length)
                {
                    CompleteTypewriter();
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (IsTyping)
                {
                    CompleteTypewriter();
                }
                else if (currentModel.Choices.Count == 0)
                {
                    AdvanceRequested?.Invoke(currentModel.SessionId);
                    facade?.Advance();
                }
                else
                {
                    ConfirmChoice(selectedChoiceIndex);
                }
            }

            if (!IsTyping && currentModel.Choices.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    selectedChoiceIndex = (selectedChoiceIndex + 1) % currentModel.Choices.Count;
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    selectedChoiceIndex = (selectedChoiceIndex - 1 + currentModel.Choices.Count) % currentModel.Choices.Count;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, shadeStyle);

            float panelHeight = Mathf.Clamp(Screen.height * 0.28f, 250f, 328f);
            float panelLeft = Mathf.Clamp(Screen.width * 0.255f, 360f, 500f);
            float panelWidth = Screen.width - panelLeft - Mathf.Clamp(Screen.width * 0.05f, 54f, 96f);
            Rect panel = new Rect(panelLeft, Screen.height - panelHeight - 36f, panelWidth, panelHeight);

            // Portrait slot: render the current speaker's portrait in the reserved left region, anchored to
            // the panel. Drawn only when the generated art is present (Codex/image2); otherwise the layout
            // is unchanged. Height-driven aspect keeps portraits from stretching.
            if (PortraitTexture != null)
            {
                float portraitHeight = Mathf.Clamp(Screen.height * 0.46f, 280f, 520f);
                float aspect = PortraitTexture.height > 0 ? (float)PortraitTexture.width / PortraitTexture.height : 0.7f;
                float portraitWidth = portraitHeight * aspect;
                Rect portraitRect = new Rect(panelLeft - portraitWidth - 18f, panel.yMax - portraitHeight, portraitWidth, portraitHeight);
                if (portraitRect.x < 12f)
                {
                    portraitRect.x = 12f;
                }

                GUI.DrawTexture(portraitRect, PortraitTexture, ScaleMode.ScaleToFit);
            }

            GUI.Box(panel, GUIContent.none, panelStyle);

            Rect nameplate = new Rect(panel.x + 34f, panel.y - 48f, 322f, 64f);
            GUI.Box(nameplate, GUIContent.none, nameplateStyle);
            GUI.Label(new Rect(nameplate.x + 18f, nameplate.y + 4f, nameplate.width - 36f, nameplate.height - 8f), SpeakerText, speakerStyle);

            bool hasChoices = currentModel.Choices.Count > 0;
            float choiceWidth = hasChoices ? Mathf.Min(390f, Mathf.Max(276f, panel.width * 0.28f)) : 0f;
            Rect textRect = new Rect(panel.x + 42f, panel.y + 68f, Mathf.Max(280f, panel.width - 42f - (hasChoices ? choiceWidth + 64f : 64f)), panel.height - 104f);
            GUI.Label(textRect, VisibleText, textStyle);

            if (hasChoices && !IsTyping)
            {
                DrawChoices(new Rect(panel.xMax - choiceWidth - 34f, panel.y + 64f, choiceWidth, panel.height - 96f));
            }
            else if (!IsTyping)
            {
                GUI.Label(new Rect(panel.xMax - 62f, panel.yMax - 54f, 40f, 36f), "▼", speakerStyle);
            }
        }

        private void DrawChoices(Rect area)
        {
            GUILayout.BeginArea(area);
            for (int index = 0; index < currentModel.Choices.Count; index++)
            {
                GUIStyle style = index == selectedChoiceIndex ? selectedChoiceStyle : choiceStyle;
                if (GUILayout.Button(currentModel.Choices[index].Text, style, GUILayout.Height(54f)))
                {
                    selectedChoiceIndex = index;
                    ConfirmChoice(index);
                }
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            shadeStyle = BoxStyle(new Color(0.02f, 0.018f, 0.015f, 0.16f));
            panelStyle = BoxStyle(new Color(0.035f, 0.030f, 0.027f, 0.90f));
            nameplateStyle = BoxStyle(new Color(0.08f, 0.046f, 0.035f, 0.96f));
            speakerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 31,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.84f, 0.46f, 1f) }
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 26,
                wordWrap = true,
                normal = { textColor = new Color(0.98f, 0.95f, 0.86f, 1f) }
            };
            choiceStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };
            selectedChoiceStyle = new GUIStyle(choiceStyle)
            {
                normal = { textColor = new Color(1f, 0.78f, 0.36f, 1f) }
            };
        }

        private static GUIStyle BoxStyle(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = texture }
            };
        }

        private static string ResolvePortraitMotion(string expression)
        {
            return (expression ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "angry" or "mad" => "snap",
                "sad" or "upset" => "dip",
                "surprised" or "surprise" or "shocked" => "pop",
                _ => "swap"
            };
        }
    }
}
