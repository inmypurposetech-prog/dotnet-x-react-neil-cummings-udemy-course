import { useEffect, useState } from 'react'
import './App.css'
import { List, ListItem, Typography } from '@mui/material';

function App() {
// js function does not have the ability to remember things - it takes something and returns something
// useState react hook is used to remember stuff in our component
// const [stateVariable, functionThatWillUpdateState] = useState from the react library
const [activities, setActivities] = useState<Activity[]>([]);

// hook that causes a side effect when the component mounts/is initialised == useEffect - what do we want to happen when our component loads
useEffect(() => {
  axios.get<Activity[]>('https://localhost:5001/api/activities')
  .then(response => setActivities(response.data))

  return () => {}
}, [])

  return (
    // only one thing can be returned here
    <>
      <Typography variant='h3'>Reactivities</Typography>
      <List>
        {activities.map((activity) => (
          <ListItem key={activity.id}>{activity.title}</ListItem>
        ))}
      </List>
    </>
  )
}

export default App
