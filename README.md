# 🎮 Plug Input Pack

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![Input System](https://img.shields.io/badge/Input%20System-1.4.0%2B-green.svg)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**PlugInputPack** é um pacote Unity robusto e reutilizável que simplifica drasticamente a integração e uso do **Unity Input System**. Com uma arquitetura modular e API intuitiva, ele permite que você configure inputs complexos em minutos, não horas.

## ✨ **Principais Características**

### 🚀 **Facilidade de Uso**
- **API ultra-simples**: `input["Move"]` para acessar qualquer input
- **Conversões automáticas**: Suporte nativo para Vector2, Vector3, float, bool, int
- **Zero configuração**: Funciona imediatamente após a importação
- **ScriptableObject**: Configurações reutilizáveis entre projetos

### 🛠️ **Recursos Avançados**
- **Sistema de debug integrado**: Logs detalhados e visualização em tempo real
- **Cache otimizado**: Performance superior com gerenciamento inteligente de memória
- **Eventos públicos avançados**: 8 tipos diferentes de eventos para máxima flexibilidade
- **Validação robusta**: Tratamento seguro de erros e configurações inválidas

### 🎯 **Compatibilidade Total**
- **Unity 2022.3+**: Suporte completo às versões LTS
- **Multiplataforma**: PC, Console, Mobile, WebGL
- **Input System 1.4.0+**: Integração nativa com o sistema moderno do Unity

## 📥 **Instalação**

### Método 1: Package Manager (Recomendado)
1. Abra o **Package Manager** (`Window > Package Manager`)
2. Clique no botão **`+`** no canto superior esquerdo
3. Selecione **`Add package from git URL...`**
4. Cole a URL:
```
https://github.com/Natteens/com.natteens.pluginputpack.git
```
5. Clique em **`Add`**

### Método 2: manifest.json
Adicione ao seu `Packages/manifest.json`:
```json
{
   "dependencies": {
      "com.natteens.pluginputpack": "https://github.com/Natteens/com.natteens.pluginputpack.git"
   }
}
```

## 🚀 **Guia de Uso Rápido**

### 1️⃣ **Configuração Inicial**

1. **Crie um Input Action Asset**
   - `Create > Input Actions`
   - Configure suas ações (Move, Jump, Attack, etc.)

2. **Crie um PlugInputReader**
   - `Create > Plug Input Pack > Input Reader`
   - Arraste seu Input Action Asset para o campo
   - Configure opções de debug se necessário

3. **Adicione o PlugInputComponent**
   - Adicione o componente a um GameObject
   - Arraste o PlugInputReader criado

### 2️⃣ **Usando nos Scripts**

```csharp
using UnityEngine;
using PlugInputPack;

public class PlayerController : MonoBehaviour
{
    private PlugInputComponent input;
    
    void Start()
    {
        input = FindObjectOfType<PlugInputComponent>();
    }
    
    void Update()
    {
        // Leitura direta com conversão automática
        Vector2 movement = input["Move"];
        bool jump = input["Jump"];
        float lookX = input["LookX"];
        
        // Ou usando propriedades específicas
        if (input["Jump"].Pressed)
        {
            // Executado apenas no frame que foi pressionado
            Jump();
        }
        
        if (input["Move"].Bool)
        {
            // Verdadeiro enquanto estiver sendo pressionado
            Move(input["Move"].Vector2);
        }
    }
    
    void Jump() => Debug.Log("Jumping!");
    void Move(Vector2 direction) => transform.Translate(direction * Time.deltaTime);
}
```

### 3️⃣ **Sistema de Eventos Avançado**

```csharp
using UnityEngine;
using PlugInputPack;

public class InputEventHandler : MonoBehaviour
{
    void OnEnable()
    {
        // Eventos básicos
        PlugInputComponent.OnInputPerformed += HandleInputPerformed;
        PlugInputComponent.OnInputCanceled += HandleInputCanceled;
        
        // Eventos específicos
        PlugInputComponent.OnInputPressed += HandleInputPressed;
        PlugInputComponent.OnInputReleased += HandleInputReleased;
        
        // Eventos por tipo de valor
        PlugInputComponent.OnInputValueChanged += HandleFloatChange;
        PlugInputComponent.OnInputVector2Changed += HandleVector2Change;
        PlugInputComponent.OnInputStateChanged += HandleBoolChange;
        
        // Eventos do sistema
        PlugInputComponent.OnInputSystemInitialized += HandleSystemInit;
        PlugInputComponent.OnInputSystemDestroyed += HandleSystemDestroy;
    }
    
    void OnDisable()
    {
        // Sempre remover os listeners!
        PlugInputComponent.OnInputPerformed -= HandleInputPerformed;
        PlugInputComponent.OnInputCanceled -= HandleInputCanceled;
        PlugInputComponent.OnInputPressed -= HandleInputPressed;
        PlugInputComponent.OnInputReleased -= HandleInputReleased;
        PlugInputComponent.OnInputValueChanged -= HandleFloatChange;
        PlugInputComponent.OnInputVector2Changed -= HandleVector2Change;
        PlugInputComponent.OnInputStateChanged -= HandleBoolChange;
        PlugInputComponent.OnInputSystemInitialized -= HandleSystemInit;
        PlugInputComponent.OnInputSystemDestroyed -= HandleSystemDestroy;
    }
    
    void HandleInputPerformed(string actionName, object value)
        => Debug.Log($"Input {actionName} executado: {value}");
    
    void HandleInputPressed(string actionName)
        => Debug.Log($"Pressionou: {actionName}");
    
    void HandleInputReleased(string actionName)
        => Debug.Log($"Soltou: {actionName}");
    
    void HandleFloatChange(string actionName, float value)
        => Debug.Log($"Float mudou {actionName}: {value}");
    
    void HandleVector2Change(string actionName, Vector2 value)
        => Debug.Log($"Vector2 mudou {actionName}: {value}");
    
    void HandleBoolChange(string actionName, bool value)
        => Debug.Log($"Bool mudou {actionName}: {value}");
    
    void HandleSystemInit()
        => Debug.Log("Sistema de Input inicializado!");
    
    void HandleSystemDestroy()
        => Debug.Log("Sistema de Input destruído!");
}
```

### 4️⃣ **Validação Segura**

```csharp
// Verificação segura de existência
if (input.HasInput("SpecialAction"))
{
    bool special = input["SpecialAction"];
}

// Ou usando TryGetInput
if (input.TryGetInput("Move", out InputAccessor moveInput))
{
    Vector2 movement = moveInput.Vector2;
}

// Listar todos os inputs disponíveis
foreach (string inputName in input.GetAllInputNames())
{
    Debug.Log($"Input disponível: {inputName}");
}
```

## 🔧 **API Completa**

### **InputAccessor - Propriedades**
```csharp
// Conversões automáticas
Vector2 movement = input["Move"];        // Conversão implícita
float axis = input["Horizontal"];        // Conversão implícita
bool button = input["Jump"];             // Conversão implícita

// Propriedades específicas
input["Move"].Vector2                    // Como Vector2
input["Move"].Vector3                    // Como Vector3
input["Move"].Float                      // Como float
input["Move"].Bool                       // Como bool
input["Move"].Int                        // Como int

// Estados de frame
input["Jump"].Pressed                    // Pressionado NESTE frame
input["Jump"].Released                   // Liberado NESTE frame
input["Jump"].IsPressed                  // Está sendo pressionado

// Informações de debug
input["Move"].RawValue                   // Valor bruto sem conversão
input["Move"].InputType                  // Tipo do input
input["Move"].Name                       // Nome da ação
```

### **PlugInputComponent - Métodos**
```csharp
// Acesso principal
InputAccessor accessor = input["ActionName"];

// Validação
bool exists = input.HasInput("ActionName");
bool success = input.TryGetInput("ActionName", out InputAccessor accessor);

// Listagem
IEnumerable<string> allInputs = input.GetAllInputNames();
```

### **Eventos Disponíveis**
```csharp
// Eventos básicos
OnInputPerformed(string actionName, object value)    // Qualquer input executado
OnInputCanceled(string actionName)                   // Qualquer input cancelado

// Eventos de estado
OnInputPressed(string actionName)                    // Quando pressiona
OnInputReleased(string actionName)                   // Quando solta

// Eventos por tipo
OnInputValueChanged(string actionName, float value)      // Mudança float
OnInputVector2Changed(string actionName, Vector2 value)  // Mudança Vector2
OnInputStateChanged(string actionName, bool value)       // Mudança bool

// Eventos do sistema
OnInputSystemInitialized()                          // Sistema iniciado
OnInputSystemDestroyed()                            // Sistema destruído
```

## ⚙️ **Configurações de Debug**

### **PlugInputReader - Opções**
- **Enable Debug**: Ativa logs detalhados no console
- **Enable Visual Debug**: Mostra interface visual em tempo real
- **Debug Handle Size**: Tamanho dos elementos visuais (1-300)
- **Debug Handle Color**: Cor dos elementos de debug

### **Debug Visual em Tempo Real**
Quando habilitado, mostra um painel com:
- ✅ Inputs ativos no momento
- 📊 Valores em tempo real
- 🎯 Indicadores visuais para Vector2/Vector3
- 📈 Histórico de atividade

## 🔄 **Changelog**

### **v1.0.2** (Atual)
- ✅ Sistema de eventos públicos expandido (8 tipos diferentes)
- ✅ Detecção inteligente de mudanças de valor
- ✅ Métodos de validação segura (`TryGetInput`, `HasInput`, `GetAllInputNames`)
- ✅ Tratamento robusto de erros em todo o sistema
- ✅ URLs corrigidas e dependências atualizadas
- ✅ Email de contato atualizado

### **v1.0.1**
- 🐛 Correção do erro de "Presse"

### **v1.0.0**
- 🎉 Lançamento inicial com base funcional completa

## 📄 **Licença**

Este projeto está licenciado sob a **MIT License** - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🙋‍♂️ **Suporte**

- **Autor**: [Nathan da Silva Miranda](https://github.com/Natteens)
- **Email**: natteens.social@gmail.com
- **Issues**: [GitHub Issues](https://github.com/Natteens/com.natteens.pluginputpack/issues)

---

<div align="center">

**⭐ Se este package te ajudou, considere dar uma estrela no repositório! ⭐**

*Feito com ❤️ por [Nathan da Silva Miranda](https://github.com/Natteens)*

</div>