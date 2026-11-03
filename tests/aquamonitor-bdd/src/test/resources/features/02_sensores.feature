# language: pt
@sensores
Funcionalidade: Gestão de sensores de consumo de água
  Como gestor de sustentabilidade
  Quero cadastrar e manter os sensores instalados nos pontos de consumo
  Para monitorar o uso de água por tipo de imóvel e acompanhar as metas ambientais

  # ESG (E - Ambiental): cada sensor tem um limite de consumo, base para detectar desperdício.
  # ESG (E - Eficiência): a listagem é paginada e limitada, economizando banco, rede e energia.

  @positivo @contrato
  Cenário: Cadastrar um sensor residencial válido
    Dado que eu preencho um sensor "Residencial" com limite de consumo de 500 litros
    Quando eu cadastro o sensor
    Então o status da resposta deve ser 201
    E a resposta deve respeitar o contrato "sensor"
    E o sensor retornado deve estar ativo
    E o cabeçalho "Location" deve apontar para o sensor criado

  @negativo @contrato
  Esquema do Cenário: Cadastro de sensor com dado inválido é rejeitado - <motivo>
    Dado que eu preencho um sensor "Residencial" com limite de consumo de 500 litros
    E o campo "<campo>" do sensor recebe o valor "<valor>"
    Quando eu cadastro o sensor
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-validacao"
    E a resposta deve apontar erro de validação no campo "<campo>"

    Exemplos:
      | motivo                      | campo         | valor    |
      | tipo de imóvel inexistente  | Tipo          | Agricola |
      | limite de consumo zerado    | LimiteConsumo | 0        |
      | nome em branco              | Nome          |          |
      | localização em branco       | Localizacao   |          |

  @positivo @contrato
  Cenário: Consultar um sensor existente pelo identificador
    Dado que existe um sensor "Comercial" cadastrado com limite de 800 litros
    Quando eu consulto o sensor pelo seu identificador
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "sensor"
    E o campo "Tipo" da resposta deve ser "Comercial"
    E o campo "LimiteConsumo" da resposta deve ser 800

  @negativo @contrato
  Cenário: Consultar um sensor inexistente
    Quando eu consulto o sensor de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Sensor 999999 não encontrado."

  @positivo @contrato @esg-eficiencia
  Cenário: Listagem de sensores é paginada e filtrada por tipo
    Dado que existem 3 sensores "Industrial" cadastrados
    Quando eu listo os sensores do tipo "Industrial" na página 1 com 2 itens por página
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "sensores-paginado"
    E a página deve conter no máximo 2 registros
    E todos os registros devem ter o campo "Tipo" igual a "Industrial"
    E a resposta deve indicar que existe uma próxima página
    E a resposta deve ser recebida em menos de 3000 ms

  @negativo @contrato @esg-eficiencia
  Cenário: Pedido de página gigante é limitado pela API para poupar recursos
    Quando eu listo os sensores na página 1 com 500 itens por página
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "sensores-paginado"
    E o campo "TamanhoPagina" da resposta deve ser 10
    E a página deve conter no máximo 10 registros

  @positivo @contrato
  Cenário: Atualizar o limite de consumo e desativar um sensor
    Dado que existe um sensor "Residencial" cadastrado com limite de 500 litros
    Quando eu reduzo o limite do sensor para 350 litros e o desativo
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "sensor"
    E o campo "LimiteConsumo" da resposta deve ser 350
    E o sensor retornado deve estar inativo

  @negativo @contrato
  Cenário: Atualizar um sensor inexistente
    Quando eu atualizo o sensor de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Sensor 999999 não encontrado."

  @positivo
  Cenário: Excluir um sensor da plataforma
    Dado que existe um sensor "Comercial" cadastrado com limite de 300 litros
    Quando eu excluo o sensor
    Então o status da resposta deve ser 204
    E o sensor não deve mais ser encontrado

  @negativo
  Cenário: Excluir um sensor inexistente
    Quando eu excluo o sensor de identificador 999999
    Então o status da resposta deve ser 404
    E a mensagem de erro deve ser "Sensor 999999 não encontrado."
