using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public abstract class HandLightComponent : MonoBehaviour 
{
    public abstract void InitComponent();
}

public class HandLightRotator : MonoBehaviour
{
    [Tooltip("Please Find HandLight GameObject and Refer on it!")]
    [SerializeField] GameObject handlightObject;

    Detector detector;

    private void Start()
    {
       detector = gameObject.GetComponent<Detector>();

     if(handlightObject == null)
        {
            Debug.LogError("HandLight Object가 현재 없습니다! 씬에서 player 하위에 있는 오브젝트를 찾아서 참조시켜 주세요!");
        }    
    }

    void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            handlightObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            detector.SetLookingDirection(Vector3.up);
        }
        else if(Input.GetKey(KeyCode.A))
        {
            handlightObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            detector.SetLookingDirection(Vector3.left);
        }
        else if(Input.GetKey(KeyCode.S))
        {
            handlightObject.transform.rotation = Quaternion.Euler(0, 0, 180);
            detector.SetLookingDirection(Vector3.down);
        }
        else if(Input.GetKey(KeyCode.D))
        {
            handlightObject.transform.rotation = Quaternion.Euler(0, 0, 270);
            detector.SetLookingDirection(Vector3.right);
        }
    }
}
