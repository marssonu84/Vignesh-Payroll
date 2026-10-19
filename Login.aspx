<%@ Page Language="C#" AutoEventWireup="true" Inherits="PayrollWebApp.Login" %>

<!DOCTYPE html>
<html>
<head runat="server">

    <title>Sign In - Vighnesh Payroll Systems</title>

    <meta name="viewport"
          content="width=device-width, initial-scale=1.0" />

    <!-- Google AdSense: publisher ca-pub-8404356823010331 -->

    <style>

        body {
            font-family: Arial, sans-serif;
            background-color: #f4f7f6;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
        }

        .login-container {
            background-color: #ffffff;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.12);
            width: 90%;
            max-width: 340px;
            box-sizing: border-box;
        }

        .login-container h2 {
            text-align: center;
            color: #333333;
            margin-top: 0;
            margin-bottom: 25px;
        }

        .form-group {
            margin-bottom: 18px;
        }

        .form-group label {
            display: block;
            margin-bottom: 6px;
            color: #555555;
        }

        .form-group input[type="text"],
        .form-group input[type="password"] {
            width: 100%;
            padding: 11px;
            border: 1px solid #cccccc;
            border-radius: 4px;
            box-sizing: border-box;
            font-size: 16px;
        }

        .login-button {
            width: 100%;
            padding: 11px;
            background-color: #007bff;
            color: #ffffff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
        }

        .login-button:hover {
            background-color: #0056b3;
        }

        .error-message {
            color: #c62828;
            text-align: center;
            display: block;
            margin-top: 12px;
        }

        .success-message {
            color: #1b7f3a;
            text-align: center;
            display: block;
            margin-top: 12px;
        }

        .signup-divider {
            display: flex;
            align-items: center;
            gap: 10px;
            color: #777;
            font-size: 13px;
            margin: 20px 0 14px;
        }

        .signup-divider:before,
        .signup-divider:after {
            content: "";
            flex: 1;
            height: 1px;
            background: #e1e5e9;
        }

        .signup-button {
            display: block;
            width: 100%;
            padding: 11px;
            border: 1px solid #007bff;
            color: #007bff;
            background: #ffffff;
            border-radius: 4px;
            box-sizing: border-box;
            text-align: center;
            text-decoration: none;
            font-size: 16px;
            font-weight: 600;
        }

        .signup-button:hover {
            background: #f0f7ff;
        }

        .home-link { display:block; text-align:center; margin-top:14px; color:#667085; font-size:13px; text-decoration:none; }
        .home-link:hover { color:#007bff; }

        .signup-note {
            color: #6b7280;
            font-size: 12px;
            line-height: 1.45;
            text-align: center;
            margin: 10px 0 0;
        }

    </style>

</head>

<body>

    <form id="loginForm" runat="server">

        <div class="login-container">

            <h2>Existing User Sign In</h2>

            <asp:Panel ID="pnlCredentials" runat="server">

            <div class="form-group">

                <label for="txtUsername">
                    Username
                </label>

                <input type="text"
                       id="txtUsername"
                       runat="server"
                       autocomplete="username" />

            </div>

            <div class="form-group">

                <label for="txtPassword">
                    Password
                </label>

                <input type="password"
                       id="txtPassword"
                       runat="server"
                       autocomplete="current-password" />

            </div>

            <div class="form-group">

                <asp:Button
                    ID="loginSubmitButton"
                    runat="server"
                    Text="Sign In"
                    CssClass="login-button"
                    OnClick="btnLogin_Click" />

            </div>


            <div class="signup-divider"><span>New user?</span></div>

            <a class="signup-button" href="Signup.aspx">
                Create New Account
            </a>

            <p class="signup-note">
                New users can sign up here. Existing users should continue with the normal sign in above.
            </p>

            </asp:Panel>

            <asp:Panel ID="pnlTwoStep" runat="server" Visible="false">

                <h2 style="font-size:1.1rem;">Two-Step Verification</h2>

                <p class="signup-note">
                    Enter the 6-digit code from your authenticator app.
                    If you lost your phone, enter one of your backup codes instead.
                </p>

                <div class="form-group">
                    <label for="txtCode">Verification code</label>
                    <input type="text"
                           id="txtCode"
                           runat="server"
                           autocomplete="one-time-code"
                           inputmode="numeric"
                           maxlength="12"
                           autofocus="autofocus" />
                </div>

                <div class="form-group">
                    <asp:Button
                        ID="btnVerify"
                        runat="server"
                        Text="Verify and Sign In"
                        CssClass="login-button"
                        OnClick="btnVerify_Click" />
                </div>

                <asp:LinkButton
                    ID="btnBack2FA"
                    runat="server"
                    Text="Start over"
                    CssClass="home-link"
                    CausesValidation="false"
                    OnClick="btnBack2FA_Click" />

            </asp:Panel>

            <a class="home-link" href="index.html">← Back to Home</a>

            <asp:Label
                ID="lblSuccess"
                runat="server"
                CssClass="success-message" />

            <asp:Label
                ID="lblError"
                runat="server"
                CssClass="error-message" />

        </div>

    </form>

<script src="Content/visit-music-continuous.js?v=10.6.3"></script></body>
</html>
