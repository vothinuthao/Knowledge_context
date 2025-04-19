using UnityEngine;

namespace CameraTool
{
    public class CameraControls : MonoBehaviour
    {
        public float panSpeed = 20f;
        public float rotationSpeed = 50f;
        public float zoomSpeed = 5f;
    
        public float minY = 5f;
        public float maxY = 30f;
    
        void Update()
        {
            // Pan with WASD
            if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.forward * (panSpeed * Time.deltaTime), Space.World);
            if (Input.GetKey(KeyCode.S)) transform.Translate(-Vector3.forward * (panSpeed * Time.deltaTime), Space.World);
            if (Input.GetKey(KeyCode.A)) transform.Translate(-Vector3.right * (panSpeed * Time.deltaTime), Space.World);
            if (Input.GetKey(KeyCode.D)) transform.Translate(Vector3.right * (panSpeed * Time.deltaTime), Space.World);
        
            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed * 100f * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        
            // Optional: Rotate with Q/E
            if (Input.GetKey(KeyCode.Q)) transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.E)) transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}