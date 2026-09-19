using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

// Functional layer for the front of the card. The existing art can stay as it is;
// this script only adds the contact actions, profile interaction and media controls.
[DisallowMultipleComponent]
public sealed class FrontExperienceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas worldCanvas;

    [Header("Contact")]
    [SerializeField] private string githubUrl = "https://github.com/J4ck4b14";
    [SerializeField] private string linkedInUrl = "https://www.linkedin.com/in/juan-abia-merino";
    [SerializeField] private string artStationUrl = "https://juanabiamerino.artstation.com";
    [SerializeField] private string cvUrl = "";
    [SerializeField] private string emailAddress = "juan.abia.merino@gmail.com";
    [SerializeField] private string phoneNumber = "";

    [Header("Media - assign either one")]
    [SerializeField] private VideoClip profileVideo;
    [SerializeField] private AudioClip profileAudio;

    [Header("Fallback interface")]
    [SerializeField] private bool buildFallbackInterface = true;
    [SerializeField, Min(0.05f)] private float profileFlipDuration = 0.22f;

    private AudioSource audioSource;
    private VideoPlayer videoPlayer;
    private RawImage videoPreview;
    private Text profileText;
    private Button cvButton;
    private Button playButton;
    private Button pauseButton;
    private Button stopButton;
    private RectTransform profilePanel;
    private RenderTexture videoTexture;
    private Coroutine profileFlipRoutine;
    private bool showingContactSide;

    private const string SummaryText =
        "JUAN ABIA MERINO\n" +
        "TECHNICAL ARTIST | TECHNICAL DESIGNER\n\n" +
        "Gameplay systems, prototyping, shaders, VFX, tools and asset integration.";

    private string ContactText =>
        "CONTACT\n" +
        emailAddress +
        (string.IsNullOrWhiteSpace(phoneNumber) ? "" : "\n" + phoneNumber) +
        "\n\nTap PROFILE to go back.";

    private void Awake()
    {
        if (worldCanvas == null)
            worldCanvas = GetComponentInChildren<Canvas>(true);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        if (buildFallbackInterface && worldCanvas != null && worldCanvas.transform.Find("FrontActions") == null)
            BuildInterface();

        RefreshMediaButtons();
    }

    private void OnDisable()
    {
        // Losing the marker should never leave audio playing in the background.
        PauseMedia();
    }

    private void OnDestroy()
    {
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }

    public void OpenGitHub() => OpenUrl(githubUrl);
    public void OpenLinkedIn() => OpenUrl(linkedInUrl);
    public void OpenArtStation() => OpenUrl(artStationUrl);
    public void OpenCv() => OpenUrl(cvUrl);

    public void ComposeEmail()
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return;

        Application.OpenURL($"mailto:{emailAddress}");
    }

    public void CallPhone()
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return;

        Application.OpenURL($"tel:{phoneNumber}");
    }

    public void ToggleProfile()
    {
        if (profilePanel == null || profileText == null)
            return;

        if (profileFlipRoutine != null)
            StopCoroutine(profileFlipRoutine);

        profileFlipRoutine = StartCoroutine(FlipProfile());
    }

    public void PlayMedia()
    {
        if (profileVideo != null)
        {
            PrepareVideo();
            videoPlayer.Play();

            if (videoPreview != null)
                videoPreview.gameObject.SetActive(true);

            return;
        }

        if (profileAudio == null)
            return;

        if (audioSource.clip != profileAudio)
            audioSource.clip = profileAudio;

        audioSource.Play();
    }

    public void PauseMedia()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void StopMedia()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (videoPreview != null)
                videoPreview.gameObject.SetActive(false);
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Application.OpenURL(url);
    }

    private IEnumerator FlipProfile()
    {
        Quaternion start = profilePanel.localRotation;
        Quaternion edge = Quaternion.Euler(0f, 90f, 0f);
        float halfDuration = Mathf.Max(0.025f, profileFlipDuration * 0.5f);

        yield return RotatePanel(start, edge, halfDuration);

        showingContactSide = !showingContactSide;
        profileText.text = showingContactSide ? ContactText : SummaryText;

        // I swap the text at 90 degrees, when the panel is practically edge-on.
        // Saves me from keeping two copies of the same panel in sync.
        yield return RotatePanel(edge, Quaternion.identity, halfDuration);

        profileFlipRoutine = null;
    }

    private IEnumerator RotatePanel(Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            profilePanel.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        profilePanel.localRotation = to;
    }

    private void PrepareVideo()
    {
        if (videoTexture == null)
        {
            videoTexture = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32)
            {
                name = "FrontProfileVideo"
            };
            videoTexture.Create();
        }

        videoPlayer.clip = profileVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;

        if (videoPreview != null)
            videoPreview.texture = videoTexture;
    }

    private void RefreshMediaButtons()
    {
        bool available = profileVideo != null || profileAudio != null;

        if (playButton != null) playButton.interactable = available;
        if (pauseButton != null) pauseButton.interactable = available;
        if (stopButton != null) stopButton.interactable = available;
        if (cvButton != null) cvButton.interactable = !string.IsNullOrWhiteSpace(cvUrl);
    }

    private void BuildInterface()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform actionsRoot = CreateRect("FrontActions", worldCanvas.transform);
        actionsRoot.anchorMin = Vector2.zero;
        actionsRoot.anchorMax = Vector2.one;
        actionsRoot.offsetMin = Vector2.zero;
        actionsRoot.offsetMax = Vector2.zero;

        profilePanel = CreateRect("ProfilePanel", actionsRoot);
        profilePanel.anchorMin = new Vector2(0.08f, 0.42f);
        profilePanel.anchorMax = new Vector2(0.92f, 0.88f);
        profilePanel.offsetMin = Vector2.zero;
        profilePanel.offsetMax = Vector2.zero;

        Image profileBackground = profilePanel.gameObject.AddComponent<Image>();
        profileBackground.color = new Color(0.02f, 0.03f, 0.08f, 0.72f);

        Button profileButton = profilePanel.gameObject.AddComponent<Button>();
        profileButton.targetGraphic = profileBackground;
        profileButton.onClick.AddListener(ToggleProfile);

        profileText = CreateText("ProfileText", profilePanel, font, SummaryText);
        profileText.alignment = TextAnchor.MiddleCenter;

        videoPreview = CreateRawImage("VideoPreview", actionsRoot);
        RectTransform previewRect = videoPreview.rectTransform;
        previewRect.anchorMin = new Vector2(0.18f, 0.40f);
        previewRect.anchorMax = new Vector2(0.82f, 0.88f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        videoPreview.gameObject.SetActive(false);

        CreateButton(actionsRoot, font, "GITHUB", 0.04f, 0.20f, 0.27f, 0.34f, OpenGitHub);
        CreateButton(actionsRoot, font, "LINKEDIN", 0.28f, 0.20f, 0.51f, 0.34f, OpenLinkedIn);
        CreateButton(actionsRoot, font, "ARTSTATION", 0.52f, 0.20f, 0.75f, 0.34f, OpenArtStation);
        CreateButton(actionsRoot, font, "EMAIL", 0.76f, 0.20f, 0.96f, 0.34f, ComposeEmail);

        cvButton = CreateButton(actionsRoot, font, "CV", 0.04f, 0.05f, 0.19f, 0.17f, OpenCv);
        CreateButton(actionsRoot, font, "PROFILE", 0.20f, 0.05f, 0.39f, 0.17f, ToggleProfile);
        playButton = CreateButton(actionsRoot, font, "PLAY", 0.40f, 0.05f, 0.57f, 0.17f, PlayMedia);
        pauseButton = CreateButton(actionsRoot, font, "PAUSE", 0.58f, 0.05f, 0.76f, 0.17f, PauseMedia);
        stopButton = CreateButton(actionsRoot, font, "STOP", 0.77f, 0.05f, 0.96f, 0.17f, StopMedia);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Text CreateText(string objectName, RectTransform parent, Font font, string value)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = new Vector2(0.04f, 0.06f);
        rect.anchorMax = new Vector2(0.96f, 0.94f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 18;
        text.raycastTarget = false;
        return text;
    }

    private static RawImage CreateRawImage(string objectName, RectTransform parent)
    {
        RectTransform rect = CreateRect(objectName, parent);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateButton(
        RectTransform parent,
        Font font,
        string label,
        float minX,
        float minY,
        float maxX,
        float maxY,
        UnityAction onClick)
    {
        RectTransform rect = CreateRect(label + "Button", parent);
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.03f, 0.07f, 0.12f, 0.82f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateText("Label", rect, font, label);
        text.alignment = TextAnchor.MiddleCenter;

        return button;
    }
}
