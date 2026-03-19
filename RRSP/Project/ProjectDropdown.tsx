import * as React from 'react'
import { Lite, getToString, liteKey } from '@framework/Signum.Entities'
import { NavDropdown } from 'react-bootstrap';
import { useAPI } from '@framework/Hooks';
import { Finder } from '@framework/Finder';
import { Navigator } from '@framework/Navigator';
import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as AppContext from "@framework/AppContext"
import SelectorModal from '@framework/SelectorModal';
import { DateTime } from 'luxon';
import './ProjectDropdown.css';
import { UserQueryEntity } from '@extensions/Signum.UserQueries/Signum.UserQueries';
import { UserQueryClient } from '@extensions/Signum.UserQueries/UserQueryClient';
import { FindOptions } from '@framework/Search';
import { ProjectEntity, MemberEntity, ProjectMessage, DomainState, IDomainEntity } from '../../Meros/Meros.Project/Meros.Project';
import { BoardEntity, DomainTaskMixin } from '../../Meros/Meros.Tasks/Meros.Tasks';
import { StakeholderEntity } from '../../Meros/Meros.Stakeholder/Meros.Stakeholder';
import { MeetingProtocolEntity } from '../../Meros/Meros.Protocol/Meros.Protocol';
import { StatusReportEntity, SpecialUserQueryType, UserQueryMixin } from '../../Meros/Meros.StatusReport/Meros.StatusReport';
import { DomainRiskMixin, RiskEntity } from '../../Meros/Meros.Risk/Meros.Risk';
import { TasksClient } from '../../Meros/Meros.Tasks/TasksClient';
import { Type, typeAllowedInDomain} from '@framework/Reflection';
import { DashboardClient } from '../../Framework/Extensions/Signum.Dashboard/DashboardClient';
import { STORAGE_KEY } from '../../Framework/Signum/React/Components/ThemeModeSelector';
import { ProgramEntity } from '../../Meros/Meros.Project/Program/Meros.Project.Program';
import { PortfolioEntity } from '../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio';

export default function ProjectDropdown(props: {}): React.JSX.Element | null {
  const [openedLastTime, setOpenedLastTime] = useState<DateTime | undefined>(undefined);

  function useThemeMode() {
    return (localStorage.getItem(STORAGE_KEY)) as "dark" | "light" ?? document.body.dataset.bsTheme;
  }

  var foDomain: FindOptions = {
    queryName: MemberEntity,
    filterOptions: [
      { token: MemberEntity.token(p => p.user), value: AppContext.currentUser },
      { token: MemberEntity.token(p => p.domain.entity?.state), value: DomainState.value('Active') },
      { token: MemberEntity.token(p => p.entity.onlyForAccess), operation: 'DistinctTo', value: true},
      {
        groupOperation: "And",
        filters: [
          {
            groupOperation: "Or",
            filters: [
              { token: MemberEntity.token(p => p.entity.domain).cast(ProjectEntity), value: null },
              { token: MemberEntity.token(p => p.entity.domain).cast(ProjectEntity).mixin(DomainTaskMixin).append(a => a.dontShowInMyWorkDropDownMenu), value: false },
            ]
          },
          {
            groupOperation: "Or",
            filters: [
              {
                token: MemberEntity.token(p => p.entity.domain).cast(ProgramEntity), value: null
              },
              { token: MemberEntity.token(p => p.entity.domain).cast(ProgramEntity).mixin(DomainTaskMixin).append(a => a.dontShowInMyWorkDropDownMenu), value: false },
            ]
          }
        ]
      }
    ],
  };
  var domains = useAPI(signal => Finder.getResultTableTyped(foDomain, {
    domain: MemberEntity.token(p => p.domain),
  }), [openedLastTime], { avoidReset: true })?.map(a => a.domain);

  const domainProjects = domains?.filter(dm => ProjectEntity.isLite(dm));
  const domainPrograms = domains?.filter(dm => ProgramEntity.isLite(dm));
  const domainPortfolios = domains?.filter(dm => PortfolioEntity.isLite(dm));

  const projectsData = useDomainData(ProjectEntity, domainProjects);
  const programsData = useDomainData(ProgramEntity, domainPrograms);
  const portfoliosData = useDomainData(PortfolioEntity, domainPortfolios);

  const domainData = { ...projectsData, ...programsData, ...portfoliosData };

  function useDomainData<T extends IDomainEntity>(entityType: Type<T>, domains: Lite<T>[] | undefined) {
    return useAPI(signal => openedLastTime == undefined || domains == null || domains.length === 0 ? Promise.resolve(undefined) : Finder.getResultTableTyped({
      queryName: entityType,
      filterOptions: [
        { token: entityType.token(p => p.entity), operation: "IsIn", value: domains }
      ]
    }, {
      entity: entityType.token(p => p.entity),
      hasStakeholderAccess: Navigator.isViewable(StakeholderEntity)
        ? entityType.token(p => p.entity).expression<boolean>("HasStakeholderAccess")
        : undefined,
      stakeholdersCount: Navigator.isViewable(StakeholderEntity)
        ? entityType.token(p => p.entity).expression<StakeholderEntity[]>("Stakeholders").count()
        : undefined,
      protocolCount: Navigator.isViewable(MeetingProtocolEntity)
        ? entityType.token(p => p.entity).expression<MeetingProtocolEntity[]>("MeetingProtocols").count()
        : undefined,
      risksCount: Navigator.isViewable(RiskEntity)
        ? entityType.token(p => p.entity).expression<RiskEntity[]>("RisksAndChances").count()
        : undefined,
    }),
      [openedLastTime, domains?.length],
      { avoidReset: true }
    )?.toObject(a => liteKey(a.entity));
  }


  const projectBoardsData = useBoardData(ProjectEntity, domainProjects);
  const programBoardsData = useBoardData(ProgramEntity, domainPrograms);
  const portfolioBoardsData = useBoardData(PortfolioEntity, domainPortfolios);

  const boardsData = { ...projectBoardsData, ...programBoardsData, ...portfolioBoardsData };

  function useBoardData<T extends IDomainEntity>(entityType: Type<T>, domains: Lite<T>[] | undefined) {
    return useAPI(signal => openedLastTime == undefined || domains == null || !Navigator.isViewable(BoardEntity) || domains.length === 0
          ? Promise.resolve(undefined)
          : Finder.getResultTableTyped({
            queryName: entityType,
            filterOptions: [
              { token: entityType.token(p => p.entity), operation: "IsIn", value: domains },
            ]
          }, {
            entity: entityType.token(p => p.entity),
            board: entityType.token(p => p.entity).expression<BoardEntity[]>("Boards").element(),
          }),
      [openedLastTime, domains?.length],
      { avoidReset: true }
    )?.groupBy(a => liteKey(a.entity)).toObject(gr => gr.key, gr => gr.elements.map(a => a.board).notNull());
  }

  const projectDashboard = useAPI(signal => DashboardClient.API.forEntityType("Project"), [])?.[0];
  const programDashboard = useAPI(signal => DashboardClient.API.forEntityType("Program"), [])?.[0];
  const portfolioDashboard = useAPI(signal => DashboardClient.API.forEntityType("Portfolio"), [])?.[0];
 
  var currentUrl = AppContext.location().pathname;
  if (domains?.length == 0)
    return null;

  function renderDomainItem(domain: Lite<IDomainEntity>) {

    const data = domainData?.[liteKey(domain)];

    const boards = boardsData?.[liteKey(domain)];

    const dashboards =
      ProjectEntity.isLite(domain) ? projectDashboard :
        ProgramEntity.isLite(domain) ? programDashboard :
          PortfolioEntity.isLite(domain) ? portfolioDashboard :
            null;

    const url = dashboards ? DashboardClient.dashboardUrl(dashboards, domain) : Navigator.navigateRoute(domain);
    const inactivColor ="var(--bs-nav-link-color)";

    return (
      <NavDropdown.Item
        key={domain.id}
        className="d-flex align-middle"
        active={url == currentUrl}>
        <button
          type="button"
          className="project-btn-reset project-name-container"
          onClick={() => AppContext.navigate(url)}
          style={{textAlign: "left"}}
          title={getToString(domain)}>
          {getToString(domain).etc(30)!}
        </button>

        {Navigator.isViewable(BoardEntity) && typeAllowedInDomain(BoardEntity, domain) && (
          <span className="project-icon-container">
            {boards && (
              <button
                type="button"
                disabled={boards.length == 0}
                aria-disabled={boards.length == 0}
                className="project-btn-reset"
                title={BoardEntity.nicePluralName()}
                style={{
                  cursor: boards.length ? "pointer" : "default",
                }}
                onClick={() => {

                  SelectorModal.chooseLite(BoardEntity, boards).then(
                    board => {
                      if (board)
                        AppContext.navigate(
                          TasksClient.getBoardUrl(board, domain)
                        );
                    }
                  );
                }}>
                <FontAwesomeIcon icon="chart-kanban" color={boards.length ? "var(--bs-indigo)" : inactivColor} aria-hidden={true} />
              </button>
            )}
          </span>
        )}

        {Navigator.isViewable(MeetingProtocolEntity) && typeAllowedInDomain(MeetingProtocolEntity, domain) && (
          <span className="project-icon-container">
            <button
              type="button"
              className="project-btn-reset"
              title={MeetingProtocolEntity.nicePluralName()}
              onClick={() =>
                Finder.explore({
                  queryName: MeetingProtocolEntity,
                  filterOptions: [
                    {
                      token: MeetingProtocolEntity.token(a => a.domain),
                      value: domain,
                      frozen: true,
                    },
                  ],
                })
              }>
              <FontAwesomeIcon icon="messages" color={data?.protocolCount ? "var(--bs-green)" : inactivColor} aria-hidden={true} />
            </button>
          </span>
        )}

        {Navigator.isViewable(RiskEntity) && typeAllowedInDomain(RiskEntity, domain) && (
          <span className="project-icon-container">
            <button
              type="button"
              className="project-btn-reset"
              title={RiskEntity.nicePluralName()}
              onClick={() =>
                AppContext.navigate(`/riskManagement/${liteKey(domain)}`)
              }>
              <FontAwesomeIcon icon="triangle-exclamation" color={data?.risksCount ? "var(--bs-orange)" : inactivColor} aria-hidden={true} />
            </button>
          </span>
        )}

        {Navigator.isViewable(StakeholderEntity) && typeAllowedInDomain(StakeholderEntity, domain) && (
          <span className="project-icon-container">
            {data?.hasStakeholderAccess
              && (
              <button
                type="button"
                className="project-btn-reset"
                title={StakeholderEntity.nicePluralName()}
                onClick={() =>
                  AppContext.navigate(
                    `/stakeholderManagement/${liteKey(domain)}`
                  )
                }>
                <FontAwesomeIcon icon="people-roof" color={data?.risksCount ? "var(--bs-info-text-emphasis)" : inactivColor} aria-hidden={true} />
              </button>
            )}
          </span>
        )}
      </NavDropdown.Item>
    );
  }

  const mode = useThemeMode();

  // Hauptkomponente  
  return (
    <NavDropdown
      id="projectsDropdown"
      title={
        <span>
          <FontAwesomeIcon icon="lightbulb" color={mode === "dark" ? "gold" : "#9f6800"} className="me-2" />
          {ProjectMessage.MyWork.niceToString()}
        </span>
      }
      onClick={() => setOpenedLastTime(DateTime.now)}>
      <div
        style={{
          maxHeight: (window.visualViewport?.height ?? 600) - 100,
          overflowY: "auto",
        }}>
        {domains && (
          <>
            {domainPortfolios && domainPortfolios.length > 0 && (
              <>
                <NavDropdown.Header>{PortfolioEntity.nicePluralName()}</NavDropdown.Header>
                {domainPortfolios.orderBy(d => getToString(d)).map(domain =>
                  renderDomainItem(domain)
                )}
                <NavDropdown.Divider />
              </>
            )}

            {domainPrograms && domainPrograms.length > 0 && (
              <>
                <NavDropdown.Header>{ProgramEntity.nicePluralName()}</NavDropdown.Header>
                {domainPrograms.orderBy(d => getToString(d)).map(domain =>
                  renderDomainItem(domain)
                )}
                <NavDropdown.Divider />
              </>
            )}

            {domainProjects && domainProjects.length > 0 && (
              <>
                <NavDropdown.Header>{ProjectEntity.nicePluralName()}</NavDropdown.Header>
                {domainProjects.orderBy(d => getToString(d)).map(domain =>
                  renderDomainItem(domain)
                )}
              </>
            )}
          </>
        )}
      </div>
    </NavDropdown>
  );
}

interface ProjectInfo {
  project: Lite<ProjectEntity>;
  hasStakeholderAccess: boolean;
  stakeholdersCount: number;
  risksCount: number;
  statusReportsCount: number;
}

interface BoardInfo {
  board: Lite<BoardEntity>;
  project: Lite<ProjectEntity>;
}

