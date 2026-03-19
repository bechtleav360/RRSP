import * as React from "react"
import { RouteObject } from 'react-router'
import { Localization } from "react-widgets-up"
import { createRoot, Root } from "react-dom/client"
import { createBrowserRouter, RouterProvider, Location } from "react-router-dom"

import * as luxon from "luxon"

import { NumberFormatSettings, reloadTypes } from "@framework/Reflection"
import * as AppContext from "@framework/AppContext"
import { AuthClient } from "@extensions/Signum.Authorization/AuthClient"
import { CultureClient } from "@framework/Basics/CultureClient"
import * as Services from "@framework/Services"

import Notify from "@framework/Frames/Notify"
import ErrorModal from "@framework/Modals/ErrorModal"

import Layout from './Layout'
import Home from './Home'
import NotFound from './NotFound'
//import { AzureADAuthenticator } from '@extensions/Signum.Authorization.AzureAD/Azure/AzureADAuthenticator'

import * as ConfigureReactWidgets from "@framework/ConfigureReactWidgets"
import { VersionChangedAlert } from "@framework/Frames/VersionChangedAlert";

import { library, config } from '@fortawesome/fontawesome-svg-core'
import { fas } from '@fortawesome/free-solid-svg-icons'
import { far } from '@fortawesome/free-regular-svg-icons'
import { fab } from '@fortawesome/free-brands-svg-icons'
import { fal } from '@fortawesome/pro-light-svg-icons'


library.add(fas);
library.add(far);
library.add(fal as any);
library.add(fab);

config.styleDefault = "fa-light";

//AzureADAuthenticator.Config.scopes.push("Calendars.Read");
//AzureADAuthenticator.Config.scopes.push("Calendars.ReadWrite");
//AzureADAuthenticator.Config.scopes.push("Mail.Send");
//AzureADAuthenticator.Config.scopes.push("Mail.ReadWrite");

AppContext.setTitleFunction(pageTitle => {
  const suffix = "RRSP";
  document.title = pageTitle ? `${pageTitle} - ${suffix}` : suffix;
});
AppContext.setTitle();


const dateLocalizer = ConfigureReactWidgets.getDateLocalizer();
const numberLocalizer = ConfigureReactWidgets.getNumberLocalizer();

Services.NotifyPendingFilter.notifyPendingRequests = pending => {
  Notify.singleton && Notify.singleton.notifyPendingRequest(pending);
}

CultureClient.onCultureLoaded.push(ci => {
  const culture = ci.name!; //"en";

  luxon.Settings.defaultLocale = culture;
  NumberFormatSettings.defaultNumberFormatLocale = culture;
}); //Culture

Services.VersionFilter.versionHasChanged = () => {
  VersionChangedAlert.forceUpdateSingletone && VersionChangedAlert.forceUpdateSingletone();
}

Services.SessionSharing.setAppNameAndRequestSessionStorage("RRSP");

// Configure AuthClient options BEFORE any authentication happens
AuthClient.Options.onLogin = (url?: string) => {
  reload().then(() => {
    const back: Location = AppContext.location()?.state?.back;
    AppContext.navigate(back ?? url ?? "/");
  });
};

AuthClient.Options.onLogout = () => {
  //AzureADAuthenticator.signOut();
  AppContext.navigate("/");
  reload();
};

// Register password validation early - lazy loaded to avoid Navigator dependency
AuthClient.validatePassword = async (password: string, user: any) => {
  const { RRSPPasswordValidation } = await import('./Globals/RRSPPasswordValidation');
  return RRSPPasswordValidation.validatePassword(password, user);
};

AuthClient.registerUserTicketAuthenticator();
//if (window.__azureADConfig) {
//  AzureADAuthenticator.registerAzureADAuthenticator();
//}

ErrorModal.register();


let root: Root | undefined = undefined;
async function reload() {
  AppContext.clearAllSettings();
  await AuthClient.autoLogin();
  await reloadTypes();
  await CultureClient.loadCurrentCulture();

  const routes: RouteObject[] = [];

  AuthClient.startPublic({ routes, userTicket: true, notifyLogout: true, });

  const isFull = Boolean(AuthClient.currentUser());

  if (isFull)
    (await import("./MainAdmin")).startFull(routes);


  const reactDiv = document.getElementById("reactDiv")!;

  if (root)
    root.unmount();

  root = createRoot(reactDiv);

  const mainRoute: RouteObject = {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Home />
      },
      ...routes,
      {
        path: "*",
        element: <NotFound />
      },
    ]
  };

  const router = createBrowserRouter([mainRoute], { basename: window.__baseName });

  AppContext.setRouter(router);

  const messages = ConfigureReactWidgets.getMessages();

  root.render(
    <Localization date={dateLocalizer} number={numberLocalizer} messages={messages} >
      <RouterProvider router={router} />
    </Localization>);

  return true;
}

reload();


