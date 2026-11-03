# language: pt
@autenticacao
Funcionalidade: Autenticação de usuários
  Como gestor de recursos hídricos
  Quero me cadastrar e me autenticar na plataforma AquaMonitor
  Para que apenas pessoas identificadas acessem alertas e relatórios de impacto hídrico

  # ESG (G - Governança): todo acesso a dados sensíveis passa por identificação do usuário.
  # ESG (S - Social): mensagens de erro claras, em português, para qualquer perfil de usuário.

  @positivo @contrato @esg-governanca
  Cenário: Cadastro de um novo usuário com dados válidos
    Dado que eu tenho os dados de um novo usuário com senha "Agua2026Bdd"
    Quando eu envio a requisição de cadastro de usuário
    Então o status da resposta deve ser 201
    E a resposta deve respeitar o contrato "auth-response"
    E o perfil do usuário retornado deve ser "Operador"
    E a resposta deve conter um token JWT válido

  @negativo @contrato
  Cenário: Cadastro com e-mail já utilizado é recusado
    Dado que existe um usuário cadastrado na plataforma
    Quando eu tento cadastrar outro usuário com o mesmo e-mail
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "E-mail já cadastrado."

  @negativo @contrato @esg-social
  Esquema do Cenário: Cadastro com dado inválido é recusado com mensagem clara - <motivo>
    Dado que eu tenho os dados de um novo usuário com senha "Agua2026Bdd"
    E o campo "<campo>" do cadastro recebe o valor "<valor>"
    Quando eu envio a requisição de cadastro de usuário
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-validacao"
    E a resposta deve apontar erro de validação no campo "<campo>"

    Exemplos:
      | motivo                    | campo | valor          |
      | senha com menos de 6 dig. | Senha | 123            |
      | e-mail em formato errado  | Email | email-invalido |
      | nome em branco            | Nome  |                |

  @positivo @contrato
  Cenário: Login com credenciais válidas devolve um token de acesso
    Dado que existe um usuário cadastrado na plataforma
    Quando eu faço login com o e-mail e a senha corretos
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "auth-response"
    E a resposta deve conter um token JWT válido

  @negativo @contrato @esg-governanca
  Cenário: Login com senha incorreta é negado
    Dado que existe um usuário cadastrado na plataforma
    Quando eu faço login com a senha "SenhaErrada999"
    Então o status da resposta deve ser 401
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "E-mail ou senha inválidos."

  @negativo
  Cenário: Login com e-mail não cadastrado é negado
    Quando eu faço login com o e-mail "nao.existe.bdd@aquamonitor.dev" e a senha "Qualquer123"
    Então o status da resposta deve ser 401
    E a mensagem de erro deve ser "E-mail ou senha inválidos."
