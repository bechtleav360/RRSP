import * as React from 'react'
import * as AppContext from "@framework/AppContext"
import { AuthClient } from '@extensions/Signum.Authorization/AuthClient'
import { useAPI } from '@framework/Hooks';
import { RRSPMessage } from './Globals/RRSP.Globals';
import { VideoEntity } from '../Meros/Meros.Videos/Meros.Videos';

const VideoBoard = React.lazy(() => import("../Meros/Meros.Videos/Templates/VideoBoard"));

export default function Home(): React.JSX.Element | null {
  var [dashboardId, setDashboardId] = React.useState<string | undefined>(undefined);
  
  React.useEffect(() => {
    if (AuthClient.currentUser()) {
      import("@extensions/Signum.Dashboard/DashboardClient")
        .then(imp => imp.DashboardClient.home())
        .then(h => {
          if (h)
            setDashboardId(h.id?.toString()!);
        });
    }

  }, []);

  return (
    <div className="container">
      <br />
      <div>
        <div className="p-5 mb-4 rounded-3">
          <img src={AppContext.toAbsoluteUrl("/PMflexOne.svg")} alt="PMflexOne" style={{
            width: "100%"
          }} />
          <h1 className='mt-5 h4' style={{ color: '#026e88' }}>MVP-Umgebung im Auftrag des Projektes PMflexOne - Wir freuen uns auf ihr Feedback</h1>
        </div>
        {!AuthClient.currentUser() ? null : dashboardId && AppContext.navigate(`/dashboard/${dashboardId}`)!}
      </div>
    </div>
  );
}
