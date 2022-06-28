using Mirror;
using System;
//using System.Timers;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof (AudioSource))]
public class VoiceChatBehavior : NetworkBehaviour
{
    [SerializeField] private float[] beforeSend = null;

    private bool micConnected = false;  
    
    private int LoopCount = 0;
    private bool LoopRepeat = false;
    //The maximum and minimum available recording frequencies  
    private int minFreq;  
    private int maxFreq;  

    public AudioSource goAudioSource;  

    private static event Action<AudioSource> OnMessage;

    // Called when the a client is connected to the server
    public override void OnStartAuthority()
    {

        if(Microphone.devices.Length <= 0)  
        {  
            //Throw a warning message at the console if there isn't  
            Debug.LogWarning("Microphone not connected!");  
        }  
        else //At least one microphone is present  
        {  
            Debug.Log("Microphone connected.");
            //Set 'micConnected' to true  
            micConnected = true;  
  
            //Get the default microphone recording capabilities  
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);  
  
            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if(minFreq == 0 && maxFreq == 0)  
            {  
                //...meaning 44100 Hz can be used as the recording sampling rate  
                maxFreq = 44100;  
            }  
  
            //Get the attached AudioSource component  
            goAudioSource = this.GetComponent<AudioSource>();
            
            //assignAuthorityObj.GetComponent<NetworkIdentity>().AssignClientAuthority(this.GetComponent<NetworkIdentity>().connectionToClient); 
        }

        OnMessage += HandleNewMessage;
    }

  

    

    // Called when a client has exited the server
    [ClientCallback]
    private void OnDestroy()
    {
        if(!hasAuthority) { return; }

        OnMessage -= HandleNewMessage;
    }

    // When a new message is added, update the Scroll View's Text to include the new message
    private void HandleNewMessage(AudioSource message)
    {
        Debug.Log("HandleNewMessage");
        message.Play();
    }

    // When a client hits the enter button, send the message in the InputField
    [Client]
    public void Send(float[] voiceData)
    {
        Debug.Log("Send");
        if(voiceData.Length == 0) { return; }
        beforeSend = voiceData;
        CmdSendMessage(voiceData);
        
    }

    [Command]
    private void CmdSendMessage(float[] message)
    {
        Debug.Log("CmdSendMessage.");
        // Validate message
        RpcHandleMessage(message);
    }

    [ClientRpc]
    private void RpcHandleMessage(float[] message)
    {
        Debug.Log("RpcHandleMessage");
        AudioSource VoiceAudio = GetComponent<AudioSource>();
        AudioClip VoiceClip = AudioClipCreateEmpty("Voice", message.Length);
        VoiceClip.SetData(message, 0);
        VoiceAudio.clip = VoiceClip;
        OnMessage?.Invoke(VoiceAudio);
    }

    public static AudioClip AudioClipCreateEmpty(string ClipName, int Length) {
        AudioClip AudioClipToReturn = AudioClip.Create (ClipName, Length, 1, 44100,false);
        return AudioClipToReturn;
    }

    [Client]
    void OnGUI()   
    {  
        //If there is a microphone  
        if(micConnected)  
        {  
            //If the audio from any microphone isn't being captured  
            if(!Microphone.IsRecording(null))  
            {  
                //Case the 'Record' button gets pressed  
                if(GUI.Button(new Rect(Screen.width-150, 125, 100, 50), "Micro: OFF"))  
                {  
                    //Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource  
                    goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);  
                    //RecordLoop = true;
                    //SetRecordLoopTimer();
                    LoopRepeat = true;       
                    
                }  
            }  
            else //Recording is in progress  
            {  
                //Case the 'Stop and Play' button gets pressed  
                if(GUI.Button(new Rect(Screen.width-150, 125, 100, 50), "Micro: ON"))  
                {  
                    
                    //CmdSendPlayerVoice();
                    //RecordLoop = false;
                    Microphone.End(null); //Stop the audio recording  
                    //goAudioSource.Play();
                    LoopRepeat = false;
                }  
  
                //GUI.Label(new Rect(Screen.width/2-100, Screen.height/2+25, 200, 50), "Recording in progress...");  
            }  
            if (LoopRepeat) {
                LoopCount++;
                if (LoopCount > 5*(1.0f/Time.deltaTime)) {
                    // one second has passed
                    Debug.Log("LoopCount: "+LoopCount+", delta: "+(5*(1.0f/Time.deltaTime)));
                    LoopCount = 0;
                    //Debug.Log("This happened after 1 second.");
                    
                    

                    if (Microphone.IsRecording(null)) {
                        Microphone.End(null);
                        //Debug.Log("micro pos: "+Microphone.GetPosition(null));
                        //Debug.Log("Micro Samples: "+goAudioSource.clip.samples);

                        //Stop the audio recording
                        Debug.Log("testing 1");
                        float[] beforeSend = new float[goAudioSource.clip.samples];
                        goAudioSource.clip.GetData(beforeSend,0);
                        Debug.Log("VoiceData prepared to be send.");
                        Send(beforeSend);
                        Debug.Log("VoiceData sent.");

                        
                    } else {
                        goAudioSource.clip = null;
                        goAudioSource = null;
                        goAudioSource = this.GetComponent<AudioSource>();
                        goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);  
                        Debug.Log("Cleaning goAudioSource "+ goAudioSource.clip.samples);
                    } 
                    
                }
            }
        }  
        else // No microphone  
        {  
            //Print a red "Microphone not connected!" message at the center of the screen  
            GUI.contentColor = Color.red;  
            GUI.Label(new Rect(Screen.width/2-100, 100, 200, 50), "Microphone NOT available!");  
        }  
  
    }

   



    public float[] VoiceData() {
        float[] beforeSend = new float[goAudioSource.clip.samples];
                    goAudioSource.clip.GetData(beforeSend,0);
                    return beforeSend;
    }

}