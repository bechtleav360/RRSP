import * as React from 'react'
import * as AppContext from "@framework/AppContext"

export default function NotFound(): React.JSX.Element {

  React.useEffect(() => {
    if (AppContext.currentUser == null) {
      AppContext.navigate("/auth/login", { state: { back: AppContext.location() }, replace: true });
    }
},[]);

    return (
      <div>
        <h1 className="h3">404 <small>Not Found</small></h1>
      </div>
);
}
