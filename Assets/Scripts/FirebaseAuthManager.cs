using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;

    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TextMeshProUGUI messageDisplay;
    [SerializeField] private TextMeshProUGUI consorcioDireccionText;
    [SerializeField] private TextMeshProUGUI consrocioLotesText;

    private string userType;


    void Awake()
    {
        InitializeFirebase();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && userType == "Admin")
        {
            SendMessageToDatabase("Mensaje de prueba enviado por un Admin.");
        }
    }


    // Función llamada cuando se presiona el botón de registro
    public void OnRegisterButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string username = usernameInput.text;

        // Validación provisoria
        if (email != "" && password.Length >= 6 && username != "")
        {
            RegisterUser(email, password, username);
        }
        else
        {
            Debug.LogError("Por favor, completa todos los campos correctamente.");
        }
    }

    // Función llamada cuando se presiona el botón de inicio de sesión
    public void OnLoginButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        // Validación provisoria
        if (email != "" && password.Length >= 6)
        {
            LoginUser(email, password);
        }
        else
        {
            Debug.LogError("Por favor, ingresa un correo electrónico y una contraseña válidos.");
        }
    }

    // Función llamada cuando se presiona el botón de crear consorcio
    public void OnConsorcioCreate()
    {
        string direccion = "Burdtwars 923";
        int lotes = 50;

        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            StartCoroutine(CreateConsorcioForUser(user, direccion, lotes));
        }
        else
        {
            Debug.LogError("No hay usuario autenticado.");
        }
    }

    // Función llamada cuando se presiona el botón de mostrar consorcio
    public void ShowConsorcio()
    {
        FirebaseUser user = auth.CurrentUser;

        StartCoroutine(GetConsorciosForUser(user));
    }

    // Función llamada cuando se presiona el botón de mostrar consorcio
    public void ModifiConsorcioDireccion()
    {
        FirebaseUser user = auth.CurrentUser;

        StartCoroutine(UpdateUserConsorcioDireccion(user, "saenz peña 1251"));
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            DependencyStatus status = task.Result;
            if (status == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                databaseRef = FirebaseDatabase.DefaultInstance.RootReference;

                Debug.Log("Firebase inicializado correctamente.");
            }
            else
            {
                Debug.LogError($"No se pudo inicializar Firebase: {status}");
            }
        });
    }

    private void LoginUser(string email, string password)
    {
        StartCoroutine(LoginCoroutine(email, password));
    }

    private IEnumerator LoginCoroutine(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError($"Error al iniciar sesión: {loginTask.Exception}");
        }
        else
        {
            FirebaseUser user = loginTask.Result.User;

            if (user.IsEmailVerified)
            {
                Debug.Log("Inicio de sesión exitoso!");
                StartCoroutine(GetUserType(user)); // Obtener el tipo de usuario
                StartListeningForMessages(); // Comenzar a escuchar mensajes en tiempo real
            }
            else
            {
                Debug.LogError("El correo electrónico no ha sido verificado. Por favor, verifica tu correo.");
            }
        }
    }

    private void RegisterUser(string email, string password, string username)
    {
        StartCoroutine(RegisterCoroutine(email, password, username));
    }

    private IEnumerator RegisterCoroutine(string email, string password, string username)
    {
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError($"Error al registrar usuario: {registerTask.Exception}");
        }
        else
        {
            FirebaseUser newUser = registerTask.Result.User;
            Debug.Log("Verifique su correo electronico!");

            StartCoroutine(SendVerificationEmail(newUser));

            SaveUserToDatabase(newUser, username, email);
        }
    }

    private IEnumerator SendVerificationEmail(FirebaseUser user)
    {
        var emailTask = user.SendEmailVerificationAsync();
        yield return new WaitUntil(() => emailTask.IsCompleted);

        if (emailTask.Exception != null)
        {
            Debug.LogError($"Error al enviar verificación: {emailTask.Exception}");
        }
        else
        {
            Debug.Log("Correo de verificación enviado!");
        }
    }

    private void SaveUserToDatabase(FirebaseUser user, string username, string email)
    {
        User newUser = new User(username, email, "Admin"); // Por defecto, el usuario se crea como "Admin"
        string json = JsonUtility.ToJson(newUser);
        databaseRef.Child("users").Child(user.UserId).SetRawJsonValueAsync(json);
    }

    private void DeleteUserFromDatabase(FirebaseUser user)
    {
        databaseRef.Child("users").Child(user.UserId).RemoveValueAsync();
        user.DeleteAsync().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Error al eliminar usuario: {task.Exception}");
            }
            else
            {
                Debug.Log("Usuario eliminado correctamente.");
            }
        });
    }

    private IEnumerator GetUserType(FirebaseUser user)
    {
        var userRef = databaseRef.Child("users").Child(user.UserId);
        var userTask = userRef.GetValueAsync();

        yield return new WaitUntil(() => userTask.IsCompleted);

        if (userTask.Exception != null)
        {
            Debug.LogError($"Error al obtener el userType: {userTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = userTask.Result;
            if (snapshot.Exists && snapshot.HasChild("userType"))
            {
                userType = snapshot.Child("userType").Value.ToString();
                Debug.Log($"UserType del usuario: {userType}");
            }
        }
    }

    private void SendMessageToDatabase(string message)
    {
        string messageId = databaseRef.Child("messages").Push().Key;
        databaseRef.Child("messages").Child(messageId).SetValueAsync(message);
    }

    private void StartListeningForMessages()
    {
        databaseRef.Child("messages").ValueChanged += (object sender, ValueChangedEventArgs args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"Error en la base de datos: {args.DatabaseError.Message}");
                return;
            }

            string latestMessage = "";
            foreach (var child in args.Snapshot.Children)
            {
                latestMessage = child.Value.ToString(); // Último mensaje
            }

            ShowMessageOnScreen(latestMessage);
        };
    }

    private void ShowMessageOnScreen(string message)
    {
        messageDisplay.text = message; // Mostrar mensaje en UI
        Debug.Log($"Mensaje en pantalla: {message}");
    }

    private IEnumerator CreateConsorcioForUser(FirebaseUser user, string direccion, int lotes)
    {
        string consorcioId = databaseRef.Child("consorcios").Push().Key;
        Consorcios newConsorcio = new Consorcios(direccion, lotes);
        string jsonConsorcio = JsonUtility.ToJson(newConsorcio);

        // Guardar el consorcio en la base de datos global
        var consorcioTask = databaseRef.Child("consorcios").Child(consorcioId).SetRawJsonValueAsync(jsonConsorcio);
        yield return new WaitUntil(() => consorcioTask.IsCompleted);

        if (consorcioTask.Exception != null)
        {
            Debug.LogError($"Error al crear consorcio: {consorcioTask.Exception}");
            yield break;
        }

        Debug.Log("Consorcio creado correctamente en la base de datos.");

        // Guardar solo la referencia al consorcio en el nodo del usuario
        var userConsorciosRef = databaseRef.Child("users").Child(user.UserId).Child("consorcios").Child(consorcioId);
        var userConsorcioTask = userConsorciosRef.SetValueAsync(true); // Guardamos solo `true` en lugar de duplicar la data
        yield return new WaitUntil(() => userConsorcioTask.IsCompleted);

        if (userConsorcioTask.Exception != null)
        {
            Debug.LogError($"Error al asociar el consorcio al usuario: {userConsorcioTask.Exception}");
        }
        else
        {
            Debug.Log("Consorcio vinculado correctamente al usuario.");
        }
    }

    private IEnumerator GetConsorciosForUser(FirebaseUser user)
    {
        var userConsorciosRef = databaseRef.Child("users").Child(user.UserId).Child("consorcios");
        var consorciosTask = userConsorciosRef.GetValueAsync();
        yield return new WaitUntil(() => consorciosTask.IsCompleted);

        if (consorciosTask.Exception != null)
        {
            Debug.LogError($"Error al obtener los consorcios del usuario: {consorciosTask.Exception}");
            yield break;
        }

        DataSnapshot consorciosSnapshot = consorciosTask.Result;

        if (!consorciosSnapshot.Exists)
        {
            Debug.Log("No hay consorcios asociados a este usuario.");
            yield break;
        }

        foreach (var consorcioSnapshot in consorciosSnapshot.Children)
        {
            string consorcioId = consorcioSnapshot.Key;

            // Ahora obtenemos la info real del consorcio desde "consorcios/{consorcioId}"
            var consorcioDataTask = databaseRef.Child("consorcios").Child(consorcioId).GetValueAsync();
            yield return new WaitUntil(() => consorcioDataTask.IsCompleted);

            if (consorcioDataTask.Exception != null)
            {
                Debug.LogError($"Error al obtener datos del consorcio {consorcioId}: {consorcioDataTask.Exception}");
                continue;
            }

            DataSnapshot consorcioDataSnapshot = consorcioDataTask.Result;
            if (consorcioDataSnapshot.Exists)
            {
                string consorcioDireccion = consorcioDataSnapshot.Child("direccion").Value.ToString();
                int consorcioLotes = int.Parse(consorcioDataSnapshot.Child("lotes").Value.ToString());

                // Mostrar los datos en los campos de UI
                consorcioDireccionText.text = "Dirección: " + consorcioDireccion;
                consrocioLotesText.text = "Lotes: " + consorcioLotes.ToString();
                Debug.Log($"Consorcio ID: {consorcioId}, Dirección: {consorcioDireccion}, Lotes: {consorcioLotes}");
            }
        }
    }

    private IEnumerator UpdateUserConsorcioDireccion(FirebaseUser user, string nuevaDireccion)
    {
        // Obtener la referencia a los consorcios del usuario actual
        var userConsorciosRef = databaseRef.Child("users").Child(user.UserId).Child("consorcios");
        var consorciosTask = userConsorciosRef.GetValueAsync();
        yield return new WaitUntil(() => consorciosTask.IsCompleted);

        if (consorciosTask.Exception != null)
        {
            Debug.LogError($"Error al obtener consorcios del usuario: {consorciosTask.Exception}");
            yield break;
        }

        DataSnapshot consorciosSnapshot = consorciosTask.Result;

        if (!consorciosSnapshot.Exists)
        {
            Debug.Log("El usuario no tiene consorcios asignados.");
            yield break;
        }

        foreach (var consorcio in consorciosSnapshot.Children)
        {
            string consorcioId = consorcio.Key;

            // Actualizar la dirección en la base de datos global de consorcios
            var consorcioRef = databaseRef.Child("consorcios").Child(consorcioId).Child("direccion");
            var updateTask = consorcioRef.SetValueAsync(nuevaDireccion);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            if (updateTask.Exception != null)
            {
                Debug.LogError($"Error al actualizar la dirección del consorcio {consorcioId}: {updateTask.Exception}");
                continue;
            }

            Debug.Log($"Dirección del consorcio {consorcioId} actualizada a: {nuevaDireccion}");
        }
    }
}
