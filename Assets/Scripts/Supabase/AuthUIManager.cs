using UnityEngine;
using TMPro;

namespace SimpleFPS
{
    public class AuthUIManager : MonoBehaviour
    {
        [Header("Giao diện")]
        public CanvasGroupController AuthCanvasGroupController;
        public CanvasGroupController FusionMenuCanvasGroupController;
        public RectTransform panel;
        public float panelLoginHeight;
        public float panelRegisterHeight;
        public TextMeshProUGUI MessageText;

        [Header("Ô nhập liệu Register")]
        public TMP_InputField RegisterEmailInput;
        public TMP_InputField RegisterUsernameInput;
        public TMP_InputField RegisterPasswordInput;
        public TMP_InputField RegisterConfirmPasswordInput;

        [Header("Ô nhập liệu Login")]
        public TMP_InputField LoginEmailInput;
        public TMP_InputField LoginPasswordInput;

        [Header("References")]
        public FusionMenuSupabaseSync fusionSync;

        [Header("Password Reset (OTP)")]
        public CanvasGroupController ResetPassCanvasGroupController;
        public TMP_InputField ResetEmailInput;
        public TMP_InputField OtpInput;
        public TMP_InputField NewPasswordInput;
        public TextMeshProUGUI ResetMessageText;

        [Header("Thông tin Vàng và Rank")]
        public TextMeshProUGUI GoldText;

        private void Start()
        {
            MessageText.text = "";
            
            // Check if already logged in (e.g., returning from match)
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                AuthCanvasGroupController.HideCanvasGroup();
                FusionMenuCanvasGroupController.ShowCanvasGroup();
            }
        }

        public void TogglePanelHeight(bool showLogin)
        {
            float targetHeight = showLogin ? panelLoginHeight : panelRegisterHeight;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, targetHeight);

            // Clear messages when switching panels
            MessageText.text = "";
        }

        public async void OnLoginClick()
        {
            // Validate inputs
            if (string.IsNullOrEmpty(LoginEmailInput.text))
            {
                MessageText.text = "Please enter an email!";
                return;
            }

            if (string.IsNullOrEmpty(LoginPasswordInput.text))
            {
                MessageText.text = "Please enter a password!";
                return;
            }

            // Check if SupabaseManager exists
            if (SupabaseManager.Instance == null)
            {
                MessageText.text = "System error: SupabaseManager not found!";
                return;
            }

            Debug.Log("Attempting login with Email: " + LoginEmailInput.text);

            MessageText.text = "Connecting to server...";

            try
            {
                bool success = await SupabaseManager.Instance.Login(LoginEmailInput.text, LoginPasswordInput.text);

                if (success)
                {
                    OnAutoLoginSuccess();
                }
                else
                {
                    MessageText.text = "Invalid email or password, or device mismatch!";
                }
            }
            catch (System.Exception ex)
            {
                MessageText.text = "Connection error!";
                Debug.LogError($"Login error: {ex.Message}");
            }
        }

        public async void OnRegisterClick()
        {
            if (string.IsNullOrEmpty(RegisterEmailInput.text))
            {
                MessageText.text = "Please enter an email!";
                return;
            }

            if (string.IsNullOrEmpty(RegisterUsernameInput.text))
            {
                MessageText.text = "Please enter a username!";
                return;
            }

            if (string.IsNullOrEmpty(RegisterPasswordInput.text))
            {
                MessageText.text = "Please enter a password!";
                return;
            }

            if (RegisterPasswordInput.text != RegisterConfirmPasswordInput.text)
            {
                MessageText.text = "Password confirmation does not match!";
                return;
            }

            // Check if SupabaseManager exists
            if (SupabaseManager.Instance == null)
            {
                MessageText.text = "Fatal System error";
                Debug.LogError("SupabaseManager instance not found in the scene!");
                return;
            }

            MessageText.text = "Creating account...";

            Debug.Log("Username: " + RegisterUsernameInput.text + " | Email: " + RegisterEmailInput.text);

            try
            {
                bool success = await SupabaseManager.Instance.Register(RegisterEmailInput.text, RegisterPasswordInput.text, RegisterUsernameInput.text);

                if (success)
                {
                    MessageText.text = "Registration successful! Please click Login.";

                    // Clear sensitive password fields
                    RegisterPasswordInput.text = "";
                    RegisterConfirmPasswordInput.text = "";
                }
                else
                {
                    MessageText.text = "Registration error (Email already exists or password too short).";
                }
            }
            catch (System.Exception ex)
            {
                MessageText.text = "Connection error!";
                Debug.LogError($"Registration error: {ex.Message}");
            }
        }

        // Called when manual login or auto login succeeds
        public void OnAutoLoginSuccess()
        {
            // Check if SupabaseManager and CurrentProfile exist
            if (SupabaseManager.Instance == null || SupabaseManager.Instance.CurrentProfile == null)
            {
                MessageText.text = "Error: Profile data not loaded!";
                Debug.LogError("Cannot load user profile - SupabaseManager or CurrentProfile is null");
                return;
            }

            MessageText.text = "Login successful!";

            AuthCanvasGroupController.HideCanvasGroup();

            // --- Save username and character to PlayerPrefs ---
            PlayerPrefs.SetString("Photon.Menu.Username", SupabaseManager.Instance.CurrentProfile.Username);
            PlayerPrefs.SetString("Photon.Menu.Character", SupabaseManager.Instance.CurrentProfile.CurrentCharacter);
            PlayerPrefs.Save();
            // --------------------------------------------------

            FusionMenuCanvasGroupController.ShowCanvasGroup();

            // Force sync username with Fusion menu after login
            if (fusionSync != null)
            {
                fusionSync.SyncUsernameFromSupabase();
            }
            var profile = SupabaseManager.Instance.CurrentProfile;
            // Giả sử bạn có 1 cái Text riêng cho Vàng và RankDisplay cho Rank
            GoldText.text = profile.Gold.ToString(); 
            GetComponentInChildren<RankDisplay>().UpdateRank(profile.RankPoints);
        }

        // Logout button handler
        public void OnLogoutClick()
        {
            // 1. Call sign out
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.SignOut();
            }

            // 2. Hide Fusion menu, show Auth panel
            FusionMenuCanvasGroupController.HideCanvasGroup();
            AuthCanvasGroupController.ShowCanvasGroup();

            // 3. Clear input fields and show message
            LoginEmailInput.text = "";
            LoginPasswordInput.text = "";
            MessageText.text = "You have safely logged out.";
        }

        // ==================== PASSWORD RESET FEATURE ====================

        public async void OnForgotPasswordClick()
        {
            if (string.IsNullOrEmpty(LoginEmailInput.text))
            {
                MessageText.text = "Please enter your email above to receive reset code!";
                return;
            }

            MessageText.text = "Sending OTP to email...";

            try
            {
                bool success = await SupabaseManager.Instance.ResetPassword(LoginEmailInput.text);

                if (success)
                {
                    // Hide Auth panel, show Reset panel
                    AuthCanvasGroupController.HideCanvasGroup();
                    ResetPassCanvasGroupController.ShowCanvasGroup();

                    ResetEmailInput.text = LoginEmailInput.text; // Copy email over
                    ResetMessageText.text = "Check your email and enter the OTP code (8 digits) here.";
                }
                else
                {
                    MessageText.text = "Error: Unable to send email.";
                }
            }
            catch (System.Exception ex)
            {
                MessageText.text = "Connection error!";
                Debug.LogError($"Reset password error: {ex.Message}");
            }
        }

        public async void OnConfirmOtpClick()
        {
            if (string.IsNullOrEmpty(OtpInput.text) || string.IsNullOrEmpty(NewPasswordInput.text))
            {
                ResetMessageText.text = "Please enter both OTP code and new password!";
                return;
            }

            ResetMessageText.text = "Verifying and changing password...";

            try
            {
                bool success = await SupabaseManager.Instance.VerifyOtpAndChangePassword(
                    ResetEmailInput.text, 
                    OtpInput.text, 
                    NewPasswordInput.text
                );

                if (success)
                {
                    // Success - return to login screen
                    ResetPassCanvasGroupController.HideCanvasGroup();
                    AuthCanvasGroupController.ShowCanvasGroup();

                    LoginPasswordInput.text = ""; // Clear old password field
                    OtpInput.text = "";
                    NewPasswordInput.text = "";
                    MessageText.text = "✅ Password changed successfully! Please log in again.";
                }
                else
                {
                    ResetMessageText.text = "❌ Error: Invalid or expired OTP code!";
                }
            }
            catch (System.Exception ex)
            {
                ResetMessageText.text = "Connection error!";
                Debug.LogError($"OTP verification error: {ex.Message}");
            }
        }

        public void OnCancelResetClick()
        {
            // Cancel button to go back to login
            ResetPassCanvasGroupController.HideCanvasGroup();
            AuthCanvasGroupController.ShowCanvasGroup();
            
            OtpInput.text = "";
            NewPasswordInput.text = "";
            ResetMessageText.text = "";
        }
    }
}