using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;
using System.IO;
using UnityEngine.Android;

public class PopupManager : MonoBehaviour
{
    public GameObject popupPrompt;
    public TextMeshProUGUI emotionText;
    public TextMeshProUGUI accuracyText;
    public RawImage cameraRawImage;
    public RawImage grayscaleImage;
    private WebCamTexture webCamTexture;

    private float timeBetweenCaptures = 2.0f;
    private float timer;
    private int activePlayer = 0;

    //private string lastResultAccuracy;
    private string highestResultAccuracy;

    private string apiURL = "your api url";

    private readonly List<string> ladderEmotions = new List<string> { "Happy", "Surprised", "Neutral" };
    private readonly List<string> snakeEmotions = new List<string> { "Anger", "Disgust", "Fear", "Sad" };
    public RawImage emojiImage;
    private Dictionary<string, Texture2D> emojiTextures;
    void LoadEmojiTextures()
    {
        emojiTextures = new Dictionary<string, Texture2D>();

        // load the textures from the Resources folder
        emojiTextures["Anger"] = Resources.Load<Texture2D>("Emojis/anger");
        emojiTextures["Disgust"] = Resources.Load<Texture2D>("Emojis/disgust");
        emojiTextures["Fear"] = Resources.Load<Texture2D>("Emojis/fear");
        emojiTextures["Happy"] = Resources.Load<Texture2D>("Emojis/happy");
        emojiTextures["Neutral"] = Resources.Load<Texture2D>("Emojis/neutral");
        emojiTextures["Sad"] = Resources.Load<Texture2D>("Emojis/sad");
        emojiTextures["Surprised"] = Resources.Load<Texture2D>("Emojis/surprised");
    }

    void Start()
    {
        // check and request camera and storage permissions
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        popupPrompt.SetActive(false);
        Debug.Log("popupPrompt active state set to false");

        timer = timeBetweenCaptures;
        StartCoroutine(APIHealthCheck());
        LoadEmojiTextures();

        Texture2D nudgeAPIImage = emojiTextures["Surprised"];
        Texture2D nonCompressedTexture = new Texture2D(nudgeAPIImage.width, nudgeAPIImage.height, TextureFormat.RGB24, false);
        nonCompressedTexture.SetPixels(nudgeAPIImage.GetPixels());
        nonCompressedTexture.Apply();
        byte[] nudgeAPIImageBytes = nonCompressedTexture.EncodeToJPG();
        Destroy(nonCompressedTexture);
        StartCoroutine(SendRequest(nudgeAPIImageBytes, "Surprised"));
    }

    private IEnumerator APIHealthCheck()
    {
        // 'using' statement is used to endure web request is properly disposed after calling the API.
        // without 'using', web request might not be correctly disposed wich leads to memory leaks
        using UnityWebRequest www = UnityWebRequest.Get(apiURL);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Health Check Failed: " + www.error);
        }
        else
        {
            Debug.Log("API Health Check Success: " + www.downloadHandler.text);
        }
    }



    public void ShowPopupPrompt(string currentTag)
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            InitializeCamera();
        }

        popupPrompt.SetActive(true);

        // set the emotion text 
        string randomEmotion = currentTag.StartsWith("Top") ? ladderEmotions[UnityEngine.Random.Range(0, ladderEmotions.Count)] :
                               currentTag.StartsWith("Tail") ? snakeEmotions[UnityEngine.Random.Range(0, snakeEmotions.Count)] :
                               "something's missing";
        emotionText.text = randomEmotion;
        Debug.Log("Emotion Set: " + randomEmotion);
        if (emojiTextures.ContainsKey(randomEmotion))
        {
            emojiImage.texture = emojiTextures[randomEmotion];
        }

        // to see which player is currently active
        // not using moveAllowed since by the time popup shows up, both players' moveAllowed are already False
        if (dice.whosTurn == 1) // when player 2's popup shows up, whosturn is already switched for player 1 hence it being 1
        {
            activePlayer = 2;
        }
        else 
        {
            activePlayer = 1;
        }
        highestResultAccuracy = "0%"; // reset the highest accuracy on every new popup
        StartCoroutine(CaptureAndSendImages());
        StartCoroutine(HidePopupAfterDelay(10));
    }

    public void InitializeCamera()
    {
        if (WebCamTexture.devices.Length > 0) // check if device has camera
        {
            webCamTexture = new WebCamTexture();

            // select front camera
            foreach (var device in WebCamTexture.devices)
            {
                if (device.isFrontFacing)
                {
                    webCamTexture.deviceName = device.name;
                    break;
                }
            }

            cameraRawImage.texture = webCamTexture; //set the texture of the RawImage to be whatever the webcam sees (webcam texture)
            cameraRawImage.material.mainTexture = webCamTexture;

            // start camera
            webCamTexture.Play();
            cameraRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -90); //by default its rotated 90 so we revert it back
            //cameraRawImage.rectTransform.localScale = new Vector3(-1, 1, 1); // flip horizontally (mirror)

            // crop out the texture to fit inside cameraRawImage square
            AdjustUVs(webCamTexture);
        }
        else
        {
            Debug.Log("No camera found on device.");
        }
    }


    void AdjustUVs(WebCamTexture webcamTexture)
    {
        float aspectRatio = (float)webcamTexture.width / webcamTexture.height;
        Rect uvRect = cameraRawImage.uvRect;

        if (aspectRatio > 1f) // if webcam is wide, e.g. laptop
        {
            float uvWidth = 1f / aspectRatio;
            //uvRect.x = (1f - uvWidth) / 2f;
            uvRect.x = (1f - uvWidth) / 2f + uvWidth;
            uvRect.width = -uvWidth; // negative width to flip horizontally
            uvRect.y = 0f;
            uvRect.height = 1f;
        }
        else // if webcam is wide, e.g. phone
        {
            float uvHeight = aspectRatio;
            uvRect.y = (1f - uvHeight) / 2f;
            uvRect.height = uvHeight;
            uvRect.x = 1f; // start from the right
            uvRect.width = -1f; // negative width to flip horizontally
        }

        cameraRawImage.uvRect = uvRect;
    }

    IEnumerator CaptureAndSendImages()
    {
        while (popupPrompt.activeSelf)
        {
            yield return new WaitForSeconds(timeBetweenCaptures);
            yield return StartCoroutine(SendCurrentImage());
        }
    }

    IEnumerator SendCurrentImage()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogError("WebCamTexture is not playing.");
            yield break;
        }

        Texture2D texture = new Texture2D(cameraRawImage.texture.width, cameraRawImage.texture.height, TextureFormat.ARGB32, false);
        texture.SetPixels(webCamTexture.GetPixels());
        texture = RotateAndMirrorTexture(texture, -90);
        texture.Apply();

        // crop the texture to 480x480
        Texture2D croppedTexture = CropTexture(texture, 480, 480);
        Destroy(texture);
        croppedTexture.Apply();
        yield return new WaitForEndOfFrame();

        // encode captured image to jpg
        byte[] imageBytes = croppedTexture.EncodeToJPG();
        // to avoid memory leaks
        Destroy(croppedTexture);
        StartCoroutine(SendRequest(imageBytes, emotionText.text));
    }

    Texture2D RotateAndMirrorTexture(Texture2D texture, float eulerAngles)
    {
        int x;
        int y;
        int i;
        int j;
        float phi = eulerAngles / (180 / Mathf.PI);
        float sn = Mathf.Sin(phi);
        float cs = Mathf.Cos(phi);
        Color32[] arr = texture.GetPixels32();
        Color32[] arr2 = new Color32[arr.Length];
        int W = texture.width;
        int H = texture.height;
        int xc = W / 2;
        int yc = H / 2;

        for (j = 0; j < H; j++)
        {
            for (i = 0; i < W; i++)
            {
                arr2[j * W + i] = new Color32(0, 0, 0, 0);

                // Apply rotation and mirror horizontally
                x = (int)(cs * (i - xc) + sn * (j - yc) + xc);
                y = (int)(-sn * (i - xc) + cs * (j - yc) + yc);
                x = W - x - 1; // Mirror horizontally

                if ((x > -1) && (x < W) && (y > -1) && (y < H))
                {
                    arr2[j * W + i] = arr[y * W + x];
                }
            }
        }

        Texture2D newImg = new Texture2D(W, H);
        newImg.SetPixels32(arr2);
        newImg.Apply();

        return newImg;
    }

    private Texture2D CropTexture(Texture2D texture, int width, int height)
    {
        int startX = (texture.width - width) / 2;
        int startY = (texture.height - height) / 2;
        Color[] pixels = texture.GetPixels(startX, startY, width, height);

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pixels);
        result.Apply();

        return result;
    }

    IEnumerator SendRequest(byte[] imageBytes, string emotion)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "image.jpg", "image/jpeg");
        form.AddField("class_name", emotion);

        // create UnityWebRequest, send the request
        using UnityWebRequest www = UnityWebRequest.Post(apiURL + "predict", form);
        yield return www.SendWebRequest();
    //|| www.result == UnityWebRequest.Result.ProtocolError
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error uploading image: " + www.error);
            // display result_accuracy
            accuracyText.text = "0%";
            // to store last result_accuracy. used for scoring later
            highestResultAccuracy = "0%";
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            long responseCode = www.responseCode;
            ProcessApiResponse(jsonResponse, responseCode);
        }
    }

    // response body OK
    [Serializable]
    public class APIResponse
    {
        public string result;
        public string result_accuracy;
        public string most_probable_class;
        public List<ClassAccuracy> all_class_accuracy;
        public string face_image;
        public string message;
    }
    [Serializable]
    public class ClassAccuracy
    {
        public string class_name;
        public string accuracy;
    }

    //response body ERROR
    [Serializable]
    public class APIErrorResponse
    {
        public string detail;
    }


    public float AccuracyToFloat(string accuracyStr)
    {
        if (float.TryParse(accuracyStr.TrimEnd('%'), out float accuracy))
        {
            return accuracy;
        }
        else
        {
            Debug.LogError("Failed to parse: " + accuracyStr); 
            return 0f;
        }
    }


    void ProcessApiResponse(string json, long responseCode)
    {
        if (responseCode == 200)
        {
            APIResponse response = JsonUtility.FromJson<APIResponse>(json);
            if (popupPrompt.activeSelf)
            {
                // display result_accuracy
                accuracyText.text = response.result_accuracy;
                // store the highest result_accuracy on every popup occurence
                if (AccuracyToFloat(response.result_accuracy) > AccuracyToFloat(highestResultAccuracy))
                {
                    highestResultAccuracy = response.result_accuracy;
                }

                StartCoroutine(DisplayGrayscaleImage(response.face_image));
            }
            else
            {
                Debug.Log(response.message); // used on start()
            }
        }
        else
        {
            APIErrorResponse errorResponse = JsonUtility.FromJson<APIErrorResponse>(json);
            // display result_accuracy
            accuracyText.text = "0%";
            Debug.Log("Detail: " + errorResponse.detail);
        }

    }

    IEnumerator DisplayGrayscaleImage(string base64Image)
    {
        byte[] imageBytes = Convert.FromBase64String(base64Image);
        Texture2D graytexture = new Texture2D(48, 48);
        graytexture.LoadImage(imageBytes);
        grayscaleImage.texture = graytexture;
        yield return null;
    }

    IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // stop the camera feed to save resources
        if (webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
        popupPrompt.SetActive(false);

        // set score with the last result_accuracy
        UpdateScore();
    }

    void UpdateScore() // this only sends the score to gamecontrol, setting it to text is done in gamecontrol
    {
        float accuracy = AccuracyToFloat(highestResultAccuracy);

        // scoring
        if (activePlayer == 1) 
        {
            GameControl.player1Score += accuracy;
            GameControl.player1Prompts++;
            Debug.Log("Player 1 score updated by popup: " + GameControl.player1Score);
        }
        else if (activePlayer == 2) 
        {
            GameControl.player2Score += accuracy;
            GameControl.player2Prompts++;
            Debug.Log("Player 2 score updated by popup: " + GameControl.player2Score);
        }
    }


    // prevent camera from staying on when game is closed/scene is changed
    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            webCamTexture = null;
            Destroy(webCamTexture);
        }
        
    }


}
