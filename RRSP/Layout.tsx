import * as React from 'react'
import { Link, Outlet, useMatch, useMatches } from 'react-router-dom'
import LoginDropdown from '@extensions/Signum.Authorization/Login/LoginDropdown'
import { AuthClient } from '@extensions/Signum.Authorization/AuthClient'
import * as AppContext from "@framework/AppContext"
import { GlobalModalContainer } from "@framework/Modals"
import Notify from "@framework/Frames/Notify"
import CultureDropdown, { CultureDropdownMenuItem } from "@framework/Basics/CultureDropdown"
import { SidebarContainer, SidebarMode, SidebarToggleItem } from "@extensions/Signum.Toolbar/SidebarContainer"
import { VersionChangedAlert, VersionInfo } from '@framework/Frames/VersionChangedAlert';
import { ErrorBoundary } from '@framework/Components';
import { OmniboxPermission } from '@extensions/Signum.Omnibox/Signum.Omnibox'
import { Breakpoints, useBreakpoint, useUpdatedRef } from '@framework/Hooks'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { ProjectEntity } from '../Meros/Meros.Project/Meros.Project'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { isPermissionAuthorized } from '@framework/AppContext'
import { STORAGE_KEY, ThemeModeSelector } from '../Framework/Signum/React/Components/ThemeModeSelector'
import { FontSizeSelector } from '../Framework/Signum/React/Components/FontSizeDropdown'
import { RRSPTourTriggers } from './Globals/RRSP.Globals';

const TourComponent = React.lazy(() => import("@extensions/Signum.Tour/TourComponent"));
const TourButton = React.lazy(() => import("@extensions/Signum.Tour/TourComponent").then(m => ({ default: m.TourButton })));
const OmniboxAutocomplete = React.lazy(() => import("@extensions/Signum.Omnibox/OmniboxAutocomplete"));
const ChangeLogViewer = React.lazy(() => import('@framework/Basics/ChangeLogViewer'));
const ProjectDropdown = React.lazy(() => import("./Project/ProjectDropdown"));
const ToolbarRenderer = React.lazy(() => import("@extensions/Signum.Toolbar/Renderers/ToolbarRenderer"));
const AlertDropdown = React.lazy(() => import("@extensions/Signum.Alerts/AlertDropdown"));
const WhatsNewDropdown = React.lazy(() => import("@extensions/Signum.WhatsNew/Dropdown/WhatsNewDropdown"));

export default function Layout(): React.JSX.Element {
  const itemStorageKey = "SIDEBAR_MODE";

  const [refreshId, setRefreshId] = React.useState(0);
  const isMobile = useBreakpoint() <= Breakpoints.sm;

  React.useEffect(() => {
    if (isMobile)
      setSidebarMode("Hidden");
    else
      setSidebarMode(window.localStorage.getItem(itemStorageKey) as SidebarMode | null ?? "Wide");
  }, [isMobile]);

  const [sidebarMode, setSidebarMode] = React.useState<SidebarMode>(isMobile ? "Hidden" : window.localStorage.getItem(itemStorageKey) as SidebarMode | null ?? "Wide");
  const sidebarModeRef = useUpdatedRef(sidebarMode);

  React.useEffect(() => {
    AppContext.Expander.onGetExpanded = () => sidebarModeRef.current != "Wide";
    AppContext.Expander.onSetExpanded = (isExpanded: boolean) => setSidebarMode(isExpanded ? (isMobile ? "Hidden" : "Narrow") : "Wide");
  }, []);

  function resetUI() {
    setRefreshId(rID => rID + 1);
  };

  React.useEffect(() => {
    AppContext.setResetUI(resetUI);
    return () => AppContext.setResetUI(() => { });
  }, []);

  const hasUser = Boolean(AuthClient.currentUser());

  function renderTitle() {
    return (
      <div className="test-environment" style={{
        transition: "all 200ms",
        padding: !hasUser ? "0 0 0 10px" : sidebarMode == "Wide" ? "8px 25px" : "8px 10px 8px 14px",
      }}>
        <Link to="/" className="navbar-brand m-0">
          <span>
            <img
              src={AppContext.toAbsoluteUrl("/PMflexOne.svg")} alt="PMflexOne"
              style={{
                height: "52px",
                maxHeight: "52px",
                aspectRatio: "1",
                objectFit: "cover",
                objectPosition: "0%",
                width: sidebarMode == "Narrow" && hasUser ? "42px" : "200px",
                marginLeft: sidebarMode == "Narrow" && hasUser ? "-7px" : undefined,
                transition: "all 200ms"
                }} /> 
          </span>
        </Link>
      </div>
    );
  }

  const [theme, setTheme] = React.useState(localStorage.getItem(STORAGE_KEY) ?? "light");

  React.useEffect(() => {
    const listener = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        setTheme(e.newValue);
      }
    };
    window.addEventListener("storage", listener);
    return () => window.removeEventListener("storage", listener);
  }, []);
  return (
    <ErrorBoundary >
      <div id="site-content" key={refreshId}>
        <Notify />
        <div id="main-container">
          <SidebarContainer
            isMobile={isMobile}
            mode={sidebarMode}
            
            sidebarContent={hasUser ? <>
              <React.Suspense fallback={JavascriptMessage.loading.niceToString()}>
                {renderTitle()}
                <ToolbarRenderer onAutoClose={isMobile ? () => setSidebarMode("Hidden") : undefined} />
                {(sidebarMode === "Wide") ?
                  <div style={{ alignSelf: "center", width: "100%", display: "flex", justifyContent: "center", padding: "10px", paddingTop: "20px",  backgroundColor: "white" }}>
                  <img alt="Bundesministerium für Digitales und Staatsmodernisierung" src={AppContext.toAbsoluteUrl("/images/BMDS_Icon_Light.svg")} style={{
                    transition: "all 200ms"
                  }}
                  />
                  </div> : undefined }
              </React.Suspense>
            </> : undefined}>
            <nav className={"main-toolbar navbar sticky-top navbar-light navbar-expand"}>
              {hasUser && <div className="navbar-nav"><SidebarToggleItem isMobile={isMobile} mode={sidebarMode} setMode={mode => {
                setSidebarMode(mode);
                if (!isMobile)
                  window.localStorage.setItem(itemStorageKey, mode);
              }} /></div>}

              {!hasUser && renderTitle()}
              <div style={{ flex: "1", marginRight: "15px" }}>
                {hasUser && isPermissionAuthorized(OmniboxPermission.ViewOmnibox) && <OmniboxAutocomplete inputAttrs={{ className: "form-control omnibox" }} />}
              </div>

              <div className="navbar-nav ml-auto me-2">
                {AuthClient.currentUser() && ProjectEntity.tryTypeInfo() && <React.Suspense fallback={null}><ProjectDropdown /></React.Suspense>}
                {hasUser && <React.Suspense fallback={null}><TourButton trigger={RRSPTourTriggers.Introduction} /></React.Suspense>}

                {hasUser && <React.Suspense fallback={null}><WhatsNewDropdown /></React.Suspense>}
                {hasUser && <React.Suspense fallback={null}><AlertDropdown /></React.Suspense>}
                <React.Suspense fallback={null}><ChangeLogViewer extraInformation={(window as any).__serverName} /></React.Suspense>
                {hasUser && <CultureDropdown isMobile={isMobile} />}
                <FontSizeSelector isMobile={isMobile} />
                <ThemeModeSelector onSetMode={mode => {
                  var navbar = document.querySelector<HTMLElement>(".navbar")!;
                  navbar.classList.toggle("bg-dark", mode == "dark");
                  navbar.classList.toggle("bg-light", mode == "light");
                  navbar.dataset.bsTheme = mode;
                  setTheme(mode);
                  localStorage.setItem(STORAGE_KEY, mode);
                  }} /*onSetMode */
                />
                <LoginDropdown renderName={u => u.userName?.tryBefore("@") ?? u.userName} changePasswordVisible={AuthClient.getAuthenticationType() != "azureAD"} />
              </div>
            </nav>
            <main id="maincontent" className="container-fluid overflow-auto pt-2" tabIndex={-1}>
              <VersionChangedAlert />
              <Outlet context={{ sidebarMode }} />
            </main>
          </SidebarContainer>
        </div>
        <GlobalModalContainer />
      </div>
    </ErrorBoundary>
  );
}
