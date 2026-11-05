using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    public VideoPlayer videoPlayer;  // Reference to VideoPlayer component
    public RawImage displayImage;    // UI Image to display the video
    public string videoURL;          // Optional: Set a URL for streaming

    void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (displayImage != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = new RenderTexture((int)videoPlayer.width, (int)videoPlayer.height, 0);
            displayImage.texture = videoPlayer.targetTexture;
        }

        // Load Video (either from VideoClip or URL)
        if (!string.IsNullOrEmpty(videoURL))
        {
            videoPlayer.url = videoURL;  // Use an online video URL
        }

        videoPlayer.Play();
    }

    public void PlayVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
    }
}
