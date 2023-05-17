using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Translations : MonoBehaviour
{
    private static int language;
    private static int previousLanguage = 0;

    public Text[] texts;
    public TextMeshProUGUI[] textsMesh;
    private static string[,] translations;
    private static Text[] textsSt;
    private static TextMeshProUGUI[] textsMeshSt;

    private static int nbrLanguages = 2;

    private void Awake()
    {
        textsSt = texts;
        textsMeshSt = textsMesh;
        translations = new string[,]
        {
            { "Settings", "Paramètres" },
            { "Scan mode", "Mode scan" },
            { "Controlled mode", "Mode télécommandé" },
            { "Connection : Waiting for robot ...", "Connexion : En attente du robot ..." },
            { "Connection : Robot connected !", "Connexion : Robot connected !" },
            { "Confirm", "Confirmer" },
            { "Cancel", "Annuler" },
            { "Robot not connected !", "Le robot n'est pas connecté !" },
            { "Robot disconnected !", "Le robot s'est déconnecté !" },
            { "Are you sure you want to exit the app ?", "Etes-vous sûr de vouloir quitter l'application ?" },
            { "Language : ", "Langue : " },
            { "Colors", "Couleurs" },
            { "General", "Généraux" },
            { "Controlled", "Télécommandé" },
            { "Color sensitivity", "Sensibilité couleurs" },
            { "Speed", "Vitesse" },
            { "Precision", "Précision" },
            { "Controls : ", "Contrôles : " },
            { "Cursors", "Curseurs" },
            { "Keyboard", "Clavier" },
            { "Controls movement :", "Mouvement contrôles :" },
            { "Freeze start position", "Bloquer position départ" },
            { "Freeze position (after start)", "Bloquer position (après départ)" },
            { "Allow movements combination", "Permettre combinaison mouvements" },
            { "Speed changes", "Changements vitesse" },
            { "Done", "Terminé" },
            { "Save", "Sauvegarde" },
            { "Exit", "Quitter" },
            { "Yes", "Oui" },
            { "No", "Non" },
            { "Are you sure you want to exit ? The map will be erased.", "Etes-vous sûr de vouloir quitter ? La carte sera effacée." },
            { "Scan options", "Options scan" },
            { "Width", "Largeur" },
            { "Height", "Hauteur" },
            { "Precision", "Précision" },
        };
    }

    public static void SetLanguage(int _language)
    {
        language = _language;
    }

    public static void UpdateTexts()
    {
        if (language != previousLanguage)
        {
            for (int i = 0; i < textsSt.Length; i++)
            {
                string text = textsSt[i].text;
                for (int j = 0; j < translations.Length / nbrLanguages; j++)
                {
                    if (translations[j, previousLanguage] == text)
                    {
                        textsSt[i].text = translations[j, language];
                        break;
                    }
                }
            }

            for (int i = 0; i < textsMeshSt.Length; i++)
            {
                string text = textsMeshSt[i].text;
                for (int j = 0; j < translations.Length / nbrLanguages; j++)
                {
                    if (translations[j, previousLanguage] == text)
                    {
                        textsMeshSt[i].text = translations[j, language];
                        break;
                    }
                }
            }
        }
        previousLanguage = language;
    }

    public static string Translate(string text, string[] parameters = null, int fromLanguage = 0)
    {
        if (language != fromLanguage)
        {
            for (int i = 0; i < translations.Length / nbrLanguages; i++)
            {
                if (translations[i, fromLanguage] == text)
                {
                    text = translations[i, language];
                    break;
                }
            }
        }

        if (text.Contains("~"))
        {
            for (int j = 0; text.Contains("~"); j++)
            {
                text = text.Replace("~" + j, parameters[j]);
            }
        }
        return text;
    }
}