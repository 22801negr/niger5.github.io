using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        ApplyLanguageStrings();
    }

    private void LoadSettings()
    {
        rbDarkTheme.IsChecked = AppSettings.Theme == "Dark";
        rbLightTheme.IsChecked = AppSettings.Theme == "Light";
        rbBlueTheme.IsChecked = AppSettings.Theme == "Blue";

        rbRussian.IsChecked = AppSettings.Language == "ru";
        rbEnglish.IsChecked = AppSettings.Language == "en";
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";

        this.Title = isEnglish ? "Settings" : "Настройки";

        if (FindName("TextSettings") is TextBlock titleText)
            titleText.Text = isEnglish ? "Settings" : "Настройки";

        if (FindName("TextTheme") is TextBlock themeText)
            themeText.Text = isEnglish ? "Theme" : "Фон";

        if (FindName("TextLanguage") is TextBlock languageText)
            languageText.Text = isEnglish ? "Language" : "Язык";

        if (FindName("rbDarkTheme") is RadioButton darkTheme)
            darkTheme.Content = isEnglish ? "Dark" : "Тёмный";

        if (FindName("rbLightTheme") is RadioButton lightTheme)
            lightTheme.Content = isEnglish ? "Light" : "Светлый";

        if (FindName("rbBlueTheme") is RadioButton blueTheme)
            blueTheme.Content = isEnglish ? "Blue" : "Синий";

        if (FindName("rbRussian") is RadioButton russian)
            russian.Content = isEnglish ? "Russian" : "Русский";

        if (FindName("rbEnglish") is RadioButton english)
            english.Content = "English";

        if (FindName("SaveButton") is Button saveButton)
            saveButton.Content = isEnglish ? "Save" : "Сохранить";

        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";
        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var newTheme = rbLightTheme.IsChecked == true
            ? "Light"
            : rbBlueTheme.IsChecked == true
                ? "Blue"
                : "Dark";

        var newLanguage = rbEnglish.IsChecked == true ? "en" : "ru";

        AppSettings.SetTheme(newTheme);
        AppSettings.SetLanguage(newLanguage);
        AppSettings.SaveSettings();
        AppSettings.ApplyThemeToAllOpenWindows();
        Localization.ApplyToAllOpenWindows();
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        WindowCaption.DragOrMaximize(this, e, MaximizeButton);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowCaption.Minimize(this);
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowCaption.ToggleMaximize(this, MaximizeButton);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
