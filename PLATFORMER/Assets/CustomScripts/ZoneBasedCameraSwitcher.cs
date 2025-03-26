using UnityEngine;
using Cinemachine;

public class ZoneBasedCameraSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera zoneCamera; // La c�mara espec�fica para la zona
    public CinemachineVirtualCamera defaultCamera; // La c�mara por defecto

    private int playerInZoneCount = 0; // Contador de cu�ntos triggers est�n activados
    private CinemachineVirtualCamera activeCamera;

    void Start()
    {
        // Inicialmente, activa la c�mara por defecto
        ActivateCamera(defaultCamera);
        activeCamera = defaultCamera;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Aseg�rate de que tu personaje tenga la etiqueta "Player"
        {
            playerInZoneCount++;
            CheckAndSwitchCamera();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZoneCount--;
            CheckAndSwitchCamera();
        }
    }

    private void CheckAndSwitchCamera()
    {
        if (playerInZoneCount > 0 && activeCamera != zoneCamera)
        {
            SwitchToCamera(zoneCamera);
        }
        else if (playerInZoneCount <= 0 && activeCamera != defaultCamera)
        {
            SwitchToCamera(defaultCamera);
        }
    }

    private void SwitchToCamera(CinemachineVirtualCamera newCamera)
    {
        if (newCamera != null && newCamera != activeCamera)
        {
            DeactivateAllCameras();
            ActivateCamera(newCamera);
            activeCamera = newCamera;
        }
    }

    private void ActivateCamera(CinemachineVirtualCamera camera)
    {
        if (camera != null)
        {
            camera.Priority = 10; // Prioridad alta para activar la c�mara
        }
    }

    private void DeactivateAllCameras()
    {
        if (zoneCamera != null)
        {
            zoneCamera.Priority = 0; // Prioridad baja para desactivar la c�mara de la zona
        }
        if (defaultCamera != null)
        {
            defaultCamera.Priority = 0; // Prioridad baja para desactivar la c�mara por defecto
        }
    }
}