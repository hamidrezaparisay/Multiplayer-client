using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [System.NonSerialized]public float h;
    [System.NonSerialized]public float v;
    public RectTransform joystick, joystickFolder, joystickBase;
    Vector3 joystickCenter = Vector3.zero;
    bool joystickState=false;
    float radius = 1.0f;
    float radiusModifier = 4.5f;
    int _pointerId = -10;
    public void OnPointerDown ( PointerEventData touchInfo )
	{
        if( joystickState == true )
			return;
        joystickState = true;
        _pointerId = touchInfo.pointerId;
        UpdateJoystick( touchInfo );
    }
    public void OnDrag ( PointerEventData touchInfo )
	{
        if( touchInfo.pointerId != _pointerId )
			return;
        UpdateJoystick( touchInfo );
	}
    public void OnPointerUp ( PointerEventData touchInfo )
	{
        if( touchInfo.pointerId != _pointerId )
			return;
        joystickState = false;
		_pointerId = -10;

        joystick.transform.position =joystickCenter;
        h=0;
        v=0;
        
    }
    void UpdateJoystick ( PointerEventData touchInfo )
    {
        Vector2 tempVector = touchInfo.position - ( Vector2 )joystickCenter;
        tempVector = Vector2.ClampMagnitude( tempVector, radius );
        Vector2 rawJoystickPosition = ( joystick.position - joystickCenter ) / radius;
        h=rawJoystickPosition.x;
        v=rawJoystickPosition.y;
        tempVector.x=0;
        joystick.transform.position = ( Vector2 )joystickCenter + tempVector;
    }
    // Start is called before the first frame update
    void OnEnable()
    {
        joystickCenter = joystickFolder.position + new Vector3( joystickFolder.sizeDelta.x / 2, joystickFolder.sizeDelta.y / 2 );
        radius = joystickFolder.sizeDelta.x * ( radiusModifier / 10 );
    }

}
