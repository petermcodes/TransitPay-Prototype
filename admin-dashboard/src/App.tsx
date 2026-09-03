/** App root: constrains the admin dashboard to the full viewport. */
import AdminApp from './AdminApp'

export default function App() {
  return (
    <div className="h-screen w-screen overflow-hidden">
      <AdminApp />
    </div>
  )
}