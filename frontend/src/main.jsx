import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import './index.css'
import Login from './pages/Login'
import TrocarSenha from './pages/TrocarSenha'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import Tutores from './pages/Tutores'
import Pets from './pages/Pets'
import PetDetalhe from './pages/PetDetalhe'
import Agenda from './pages/Agenda'
import Atendimentos from './pages/Atendimentos'
import Estoque from './pages/Estoque'
import PDV from './pages/PDV'
import MetasVendas from './pages/MetasVendas'
import Mensagens from './pages/Mensagens'
import Promocoes from './pages/Promocoes'
import Servicos from './pages/Servicos'
import Usuarios from './pages/Usuarios'
import Relatorios from './pages/Relatorios'
import Financeiro from './pages/Financeiro'
import Bancario from './pages/Bancario'
import Fornecedores from './pages/Fornecedores'
import Compras from './pages/Compras'
import Contratos from './pages/Contratos'
import ContratoDetalhe from './pages/ContratoDetalhe'
import MedicoesPendentes from './pages/MedicoesPendentes'
import Recebimentos from './pages/Recebimentos'
import CondicoesPagamento from './pages/CondicoesPagamento'
import AgendarPublico from './pages/AgendarPublico'
import AdminLogin from './pages/AdminLogin'
import AdminPanel from './pages/AdminPanel'
import Funcionarios from './pages/RH/Funcionarios'
import Fechamento from './pages/RH/Fechamento'
import RelatoriosRH from './pages/RH/RelatoriosRH'
import Parametros from './pages/Parametros'
import Bot from './pages/Parametros/Bot'
import Vias from './pages/Cadastros/Vias'
import Pelagens from './pages/Cadastros/Pelagens'
import Racas from './pages/Cadastros/Racas'
import Validar from './pages/Validar'
import DashboardVendas from './pages/DashboardVendas'
import Contabil from './pages/Contabil'
import Cargos from './pages/RH/Cargos'
import GestaoVista from './pages/GestaoVista'
import Categorias from './pages/Categorias'
import PlanosSaude from './pages/PlanosSaude'
import Trial from './pages/Trial'
import EsqueciSenha from './pages/EsqueciSenha'
import Planos from './pages/Planos'

function Protected({ children }) {
  return localStorage.getItem('token') ? children : <Navigate to="/login" />
}

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/admin/login" element={<AdminLogin />} />
        <Route path="/admin" element={<Protected><AdminPanel /></Protected>} />
        <Route path="/agendar/:token" element={<AgendarPublico />} />
        <Route path="/gestao-vista" element={<Protected><GestaoVista /></Protected>} />
        <Route path="/" element={<Protected><Layout /></Protected>}>
          <Route index element={<Dashboard />} />
          <Route path="tutores" element={<Tutores />} />
          <Route path="pets" element={<Pets />} />
          <Route path="pets/:id" element={<PetDetalhe />} />
          <Route path="agenda" element={<Agenda />} />
          <Route path="atendimentos" element={<Atendimentos />} />
          <Route path="pdv" element={<PDV />} />
          <Route path="vendas/metas" element={<MetasVendas />} />
          <Route path="estoque" element={<Estoque />} />
          <Route path="mensagens" element={<Mensagens />} />
          <Route path="promocoes" element={<Promocoes />} />
          <Route path="servicos" element={<Servicos />} />
          <Route path="usuarios" element={<Usuarios />} />
          <Route path="relatorios" element={<Relatorios />} />
          <Route path="dashboard/vendas" element={<DashboardVendas />} />
          <Route path="contabil" element={<Contabil />} />
          <Route path="financeiro" element={<Financeiro />} />
          <Route path="bancario" element={<Bancario />} />
          <Route path="fornecedores" element={<Fornecedores />} />
          <Route path="compras" element={<Compras />} />
          <Route path="contratos" element={<Contratos />} />
          <Route path="contratos/medicoes" element={<MedicoesPendentes />} />
          <Route path="contratos/:id" element={<ContratoDetalhe />} />
          <Route path="recebimentos" element={<Recebimentos />} />
          <Route path="compras/condicoes" element={<CondicoesPagamento />} />
          <Route path="rh/funcionarios" element={<Funcionarios />} />
          <Route path="rh/cargos" element={<Cargos />} />
          <Route path="rh/fechamento" element={<Fechamento />} />
          <Route path="rh/relatorios" element={<RelatoriosRH />} />
          <Route path="cadastros/vias" element={<Vias />} />
          <Route path="cadastros/pelagens" element={<Pelagens />} />
          <Route path="cadastros/racas" element={<Racas />} />
          <Route path="financeiro/categorias" element={<Categorias />} />
          <Route path="parametros" element={<Parametros />} />
          <Route path="parametros/bot" element={<Bot />} />
          <Route path="planos-saude" element={<PlanosSaude />} />
          <Route path="trocar-senha" element={<TrocarSenha />} />
        </Route>
        <Route path="/validar/:codigo" element={<Validar />} />
        <Route path="/trial" element={<Trial />} />
        <Route path="/esqueci-senha" element={<EsqueciSenha />} />
        <Route path="/redefinir-senha" element={<EsqueciSenha />} />
        <Route path="/planos" element={<Planos />} />
      </Routes>
    </BrowserRouter>
  </React.StrictMode>
)
