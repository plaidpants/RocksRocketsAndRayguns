using UnityEngine;
using UnityEngine.UI;

public class PlayerTracker : MonoBehaviour
{
    public RawImage OnScreenSprite;
    public Canvas screenCanvas;
    public Text debugText;
    public static GameObject trackingObject;
    public float xoffset;
    public float yoffset;

    public static void SetTrackingObject(GameObject track)
    {
        trackingObject = track;
    }

    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
        OnScreenSprite.enabled = false;
        debugText.enabled = false;
    }

    void Update()
    {
        if (trackingObject != null)
        {
            Vector3 screenMiddle = new Vector3(Screen.width / 2, Screen.height / 2, 0);

            Vector3 screenpos = Camera.main.WorldToScreenPoint(trackingObject.transform.position);

            float tarAngle = (Mathf.Atan2(screenpos.x - screenMiddle.x, Screen.height - screenpos.y - screenMiddle.y) * Mathf.Rad2Deg) + 90;
            if (tarAngle < 0)
            {
                tarAngle += 360;
            }

            //debugText.text = Mathf.RoundToInt(tarAngle).ToString() + " " + Mathf.RoundToInt(screenpos.x).ToString() + " " + Mathf.RoundToInt(screenpos.y).ToString() + " " + Mathf.RoundToInt(screenpos.z).ToString();
            debugText.text = "";

            if (/*(screenpos.x > Screen.width) || (screenpos.y > Screen.height) || (screenpos.y < 0) || (screenpos.x < 0) || */ (screenpos.z < 0))
            {
                OnScreenSprite.enabled = true;

                OnScreenSprite.material.SetFloat("_Alpha", 0.5f);
            }
            else
            {
                OnScreenSprite.enabled = false;
            }

            if (screenpos.z < 0)
            {
                screenpos = new Vector3(Screen.width / 2, Screen.height / 2, screenpos.z);
                tarAngle = tarAngle - 180;
            }

            if (screenpos.x > Screen.width - Screen.width * xoffset)
            {
                screenpos.x = Screen.width - Screen.width * xoffset;
            }
            else if (screenpos.x < Screen.width * xoffset)
            {
                screenpos.x = Screen.width * xoffset;
            }
            if (screenpos.y > Screen.height - Screen.height * yoffset)
            {
                screenpos.y = Screen.height - Screen.height * yoffset;
            }
            else if (screenpos.y < Screen.height * yoffset)
            {
                screenpos.y = Screen.height * yoffset;
            }

            RectTransformUtility.ScreenPointToWorldPointInRectangle(screenCanvas.GetComponent<RectTransform>(), screenpos, Camera.main, out screenpos);

            OnScreenSprite.rectTransform.position = screenpos;
            OnScreenSprite.transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0, 0, tarAngle);
        }
        else
        {
            OnScreenSprite.enabled = false;
        }
    }
}