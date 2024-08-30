using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    #region EVENTS_AND_DELEGATES
    internal event Action OnSpinClicked;
    internal event Action OnAutoSpinClicked;
    internal event Action OnAutoSpinStopClicked;
    internal event Action OnBetButtonClicked;
    internal event Action OnSpinSuccess;
    internal event Action OnBonusReceived;
    internal event Action OnFreeSpinReceived;
    internal event Action OnFreeSpinEnded;
    #endregion

    #region MANAGER_REFERENCES
    [SerializeField]
    private UIManager m_UIManager;
    [SerializeField]
    private SlotBehaviour m_SlotBehaviour;
    #endregion

    #region SERIALIZED_BOOLEANS
    [SerializeField]
    protected internal bool TurboSpin = false;
    [SerializeField]
    protected internal bool DemoFreeSpin = false;
    #endregion

    #region SERIALIZED_INTEGERS
    [SerializeField]
    protected internal int StopPos_Plus = 50;
    #endregion

    private KeyStruct m_Key;

    private void Start()
    {
        m_Key = new KeyStruct();
        InitiateButtons();
    }

    private void OnEnable()
    {
        OnFreeSpinReceived += FreeSpinAction;
        OnFreeSpinEnded += FreeSpinStopAction;
    }

    private void InitiateButtons()
    {
        m_UIManager.GetButton(m_Key.m_button_spin).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_spin).onClick.AddListener(delegate { OnSpinClicked?.Invoke(); });

        m_UIManager.GetButton(m_Key.m_button_bet_button).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_bet_button).onClick.AddListener(delegate { OnBetButtonClicked?.Invoke(); });

        m_UIManager.GetButton(m_Key.m_button_auto_spin).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_auto_spin).onClick.AddListener(delegate { OnAutoSpinClicked?.Invoke(); });

        m_UIManager.GetButton(m_Key.m_button_auto_spin_stop).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_auto_spin_stop).onClick.AddListener(delegate { OnAutoSpinStopClicked?.Invoke(); });

        m_UIManager.GetButton(m_Key.m_button_turbo_spin).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_turbo_spin).onClick.AddListener(TurboSpinClickedAction);

        m_UIManager.GetButton(m_Key.m_button_turbo_spin_stop).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_turbo_spin_stop).onClick.AddListener(TurboSpinClickedAction);

        m_UIManager.GetButton(m_Key.m_button_settings).onClick.RemoveAllListeners();
        m_UIManager.GetButton(m_Key.m_button_settings).onClick.AddListener(SettingsButtonClickedAction);
    }

    // This is the method use to trigger and off the turbo spin
    internal void TurboSpinClickedAction()
    {
        if (TurboSpin)
        {
            TurboSpin = !TurboSpin;
            m_UIManager.GetButton(m_Key.m_button_turbo_spin).gameObject.SetActive(true);
            m_UIManager.GetButton(m_Key.m_button_turbo_spin_stop).gameObject.SetActive(false);
        }
        else
        {
            TurboSpin = !TurboSpin;
            m_UIManager.GetButton(m_Key.m_button_turbo_spin).gameObject.SetActive(false);
            m_UIManager.GetButton(m_Key.m_button_turbo_spin_stop).gameObject.SetActive(true);
        }
    }

    private void SettingsButtonClickedAction()
    {

    }

    private void FreeSpinAction()
    {
        m_UIManager.GetGameObject(m_Key.m_object_reel_ref).GetComponent<Image>().sprite = m_UIManager.GetSprite(m_Key.m_sprite_free_spin_reel);
    }

    private void FreeSpinStopAction()
    {
        m_UIManager.GetGameObject(m_Key.m_object_reel_ref).GetComponent<Image>().sprite = m_UIManager.GetSprite(m_Key.m_sprite_normal_reel);
    }

    internal void InvokeFreeSpin()
    {
        m_SlotBehaviour.FreeSpin(UnityEngine.Random.Range(1, 6));
        OnFreeSpinReceived?.Invoke();
    }

    internal void InvokeFreeSpinEnd()
    {
        OnFreeSpinEnded?.Invoke();
    }

    private void OnDisable()
    {
        OnFreeSpinReceived -= FreeSpinAction;
        OnFreeSpinEnded -= FreeSpinStopAction;
    }
}

[System.Serializable]
public struct SlotImage
{
    public List<Image> slotImages;
}