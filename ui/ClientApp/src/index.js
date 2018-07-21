import "./index.css";
import React from "react";
import ReactDOM from "react-dom";
import { BrowserRouter, Route, NavLink } from "react-router-dom";
import Traces from "./screens/Traces";
import { StrategyDashboard } from "./modules/strategy-dashboard";

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");
const rootElement = document.getElementById("root");

ReactDOM.render(
  <BrowserRouter basename={baseUrl}>
    <div>
      <div>
        <NavLink to="/">Traces</NavLink>
        {' '}
        <NavLink to="/dashboard">Dashboard</NavLink>
      </div>
      <Route exact path="/" component={Traces} />
      <Route path="/dashboard" component={StrategyDashboard} />
    </div>
  </BrowserRouter>,
  rootElement
);
